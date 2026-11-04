/**
 * @file api-cliente.js
 * @description Cliente HTTP del Frontend. Centraliza todas las llamadas a la API:
 *   agrega la URL base, el token Bearer y los encabezados JSON, interpreta las respuestas
 *   { statusCode, message, data, errors } y normaliza los errores.
 * @requires config.js
 * @exports window.Api
 *
 * Toda petición rechazada entrega { status, mensaje, errores, cuerpo }, donde status 0
 * indica falta de conexión o tiempo agotado. Un 401 en una ruta protegida cierra la sesión.
 */

(function () {
  "use strict";

  const base = String(window.ELCUATE_CONFIG.API_URL || "").replace(/\/+$/, "");

  /** Mensajes por defecto según el código de estado HTTP. */
  const MENSAJES = {
    0: "No fue posible conectar con el servidor. Verifique su conexión e intente de nuevo.",
    400: "Los datos enviados no son válidos.",
    401: "Su sesión no es válida o ha expirado. Inicie sesión nuevamente.",
    403: "No tiene permisos para realizar esta acción.",
    404: "No se encontró el registro solicitado.",
    409: "La operación entra en conflicto con registros existentes.",
    429: "Demasiados intentos. Espere unos minutos e intente de nuevo.",
    500: "Ocurrió un error interno en el servidor. Intente de nuevo más tarde."
  };

  function mensajePorDefecto(status) {
    return MENSAJES[status] || (status >= 500 ? MENSAJES[500] : "No fue posible completar la operación.");
  }

  /**
   * Convierte los errores de validación del Backend a { campo: [mensajes] }.
   * Acepta el formato de ValidationProblemDetails y el de ModelState.
   * @param {object} errors Objeto "errors" de la respuesta.
   * @returns {Object<string, string[]>}
   */
  function aplanarErrores(errors) {
    const salida = {};
    if (!errors || typeof errors !== "object") return salida;
    Object.keys(errors).forEach((clave) => {
      const v = errors[clave];
      let msgs = [];
      if (Array.isArray(v)) msgs = v.map((m) => (typeof m === "string" ? m : (m && (m.errorMessage || m.message)) || ""));
      else if (v && Array.isArray(v.errors)) msgs = v.errors.map((m) => (m && (m.errorMessage || m.message)) || "");
      else if (typeof v === "string") msgs = [v];
      msgs = msgs.filter(Boolean);
      if (msgs.length) salida[clave.replace(/^.*\./, "")] = msgs;
    });
    return salida;
  }

  /**
   * Construye la URL absoluta de un endpoint. Omite los parámetros vacíos.
   * @param {string} ruta Ruta relativa, por ejemplo "api/Usuarios/GetUsuarios".
   * @param {object} [query] Parámetros de la cadena de consulta.
   * @returns {string}
   */
  function construirUrl(ruta, query) {
    let url = base + "/" + String(ruta).replace(/^\/+/, "");
    if (query) {
      const p = new URLSearchParams();
      Object.keys(query).forEach((k) => {
        const v = query[k];
        if (v !== undefined && v !== null && v !== "") p.append(k, v);
      });
      const qs = p.toString();
      if (qs) url += "?" + qs;
    }
    return url;
  }

  /**
   * Ejecuta una petición a la API.
   * @param {string} metodo GET, POST, PUT o DELETE.
   * @param {string} ruta Ruta relativa del endpoint.
   * @param {object} [opciones]
   * @param {object} [opciones.cuerpo] Cuerpo a enviar como JSON.
   * @param {object} [opciones.query] Parámetros de consulta.
   * @param {boolean} [opciones.publico] true para no enviar el token (por ejemplo, Login).
   * @returns {Promise<object|null>} Cuerpo de la respuesta.
   * @throws {{status:number, mensaje:string, errores:object, cuerpo:object}}
   */
  async function solicitar(metodo, ruta, opciones) {
    const op = opciones || {};
    const headers = { Accept: "application/json" };
    const init = { method: metodo, headers, cache: "no-store" };

    if (op.cuerpo !== undefined) {
      headers["Content-Type"] = "application/json";
      init.body = JSON.stringify(op.cuerpo);
    }
    if (!op.publico && window.Auth) {
      const token = window.Auth.token();
      if (token) headers.Authorization = "Bearer " + token;
    }

    const control = new AbortController();
    init.signal = control.signal;
    const temporizador = setTimeout(() => control.abort(), window.ELCUATE_CONFIG.TIMEOUT_MS || 20000);

    let resp;
    try {
      resp = await fetch(construirUrl(ruta, op.query), init);
    } catch (e) {
      throw { status: 0, mensaje: MENSAJES[0], errores: {}, cuerpo: null };
    } finally {
      clearTimeout(temporizador);
    }

    let cuerpo = null;
    const texto = await resp.text().catch(() => "");
    if (texto) { try { cuerpo = JSON.parse(texto); } catch (e) { cuerpo = null; } }

    if (resp.ok) return cuerpo;

    const status = resp.status;
    const errores = aplanarErrores(cuerpo && cuerpo.errors);
    let mensaje = (cuerpo && (cuerpo.message || cuerpo.title)) || mensajePorDefecto(status);
    if (status === 400 && Object.keys(errores).length) {
      mensaje = Object.keys(errores).map((k) => errores[k][0]).join(" ");
    }

    // Un 401 indica token vencido, revocado o inválido: se cierra la sesión local.
    if (status === 401 && !op.publico && window.Auth) window.Auth.sesionExpirada();

    throw { status, mensaje, errores, cuerpo };
  }

  /**
   * GET que devuelve el arreglo "data". Un 404 de listado se interpreta como lista vacía,
   * porque la API responde 404 cuando no hay registros.
   * @returns {Promise<Array>}
   */
  async function lista(ruta, query) {
    try {
      const r = await solicitar("GET", ruta, { query });
      return (r && Array.isArray(r.data)) ? r.data : [];
    } catch (e) {
      if (e && e.status === 404) return [];
      throw e;
    }
  }

  /**
   * Fecha y hora local en formato "AAAA-MM-DDTHH:mm:ss" (sin zona horaria), el formato que almacena el Backend.
   * @param {Date} [d] Fecha a formatear; por defecto, el momento actual.
   * @returns {string}
   */
  function fechaLocal(d) {
    const f = d || new Date();
    const p = (n) => String(n).padStart(2, "0");
    return f.getFullYear() + "-" + p(f.getMonth() + 1) + "-" + p(f.getDate()) + "T" +
      p(f.getHours()) + ":" + p(f.getMinutes()) + ":" + p(f.getSeconds());
  }

  /** Límites de AuditoriaCrearDTO y patrón de los campos que nunca se registran. */
  const MAX_TEXTO_CORTO = 50;
  const MAX_DATOS = 20000;
  const CLAVE_SENSIBLE = /pass|token|contrase|secret|hash|clave/i;

  /**
   * Copia profunda de un objeto sin las claves sensibles (contraseñas, tokens, hashes).
   * @param {*} valor
   * @returns {*}
   */
  function sinSensibles(valor) {
    if (Array.isArray(valor)) return valor.map(sinSensibles);
    if (valor && typeof valor === "object") {
      const c = {};
      Object.keys(valor).forEach((k) => { if (!CLAVE_SENSIBLE.test(k)) c[k] = sinSensibles(valor[k]); });
      return c;
    }
    return valor;
  }

  /**
   * Serializa los datos de auditoría respetando el máximo de 20 000 caracteres.
   * Si se excede, conserva solo los campos simples y marca el resultado con _recortado.
   * @param {object} o
   * @returns {string|undefined}
   */
  function serializarDatos(o) {
    if (!o || typeof o !== "object") return undefined;
    const limpio = sinSensibles(o);
    let texto = JSON.stringify(limpio);
    if (texto.length <= MAX_DATOS) return texto;
    const simple = { _recortado: true };
    Object.keys(limpio).forEach((k) => {
      const v = limpio[k];
      if (v === null || ["number", "boolean"].includes(typeof v)) simple[k] = v;
      else if (typeof v === "string") simple[k] = v.length > 500 ? v.slice(0, 500) + "…" : v;
    });
    texto = JSON.stringify(simple);
    return texto.length <= MAX_DATOS ? texto : JSON.stringify({ _recortado: true });
  }

  /**
   * Registra un evento en Auditorias. El Backend asigna el usuario (desde el token) y la fecha.
   * Nunca lanza error: un fallo de auditoría no debe interrumpir la operación principal.
   * @param {string} tabla Tabla o módulo afectado.
   * @param {string} accion INSERT, UPDATE, DELETE, LOGIN o LOGOUT.
   * @param {object|null} antes Estado anterior del registro.
   * @param {object|null} despues Estado nuevo del registro.
   */
  async function auditar(tabla, accion, antes, despues) {
    try {
      await solicitar("POST", "api/Auditorias/PostAuditoria", {
        cuerpo: {
          tabla: String(tabla).slice(0, MAX_TEXTO_CORTO),
          accion: String(accion).slice(0, MAX_TEXTO_CORTO),
          fecha: fechaLocal(),
          datosAnteriores: serializarDatos(antes),
          datosNuevos: serializarDatos(despues)
        }
      });
    } catch (e) {
      if (window.console) console.warn("No se pudo registrar la auditoría (" + tabla + " / " + accion + "). Código: " + (e && e.status));
    }
  }

  /** API pública del cliente HTTP. */
  window.Api = {
    solicitar,
    lista,
    auditar,
    fechaLocal,
    get: (ruta, query) => solicitar("GET", ruta, { query }),
    post: (ruta, cuerpo, op) => solicitar("POST", ruta, Object.assign({ cuerpo }, op)),
    put: (ruta, cuerpo) => solicitar("PUT", ruta, { cuerpo }),
    del: (ruta) => solicitar("DELETE", ruta),
    mensajeError: (err, porDefecto) => (err && err.mensaje) || porDefecto || mensajePorDefecto(500)
  };
})();
