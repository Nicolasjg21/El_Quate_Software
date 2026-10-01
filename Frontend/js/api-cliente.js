/* ==========================================================================
   API-CLIENTE.JS — Único punto de salida hacia el Backend (fetch centralizado)
   --------------------------------------------------------------------------
   Todas las llamadas HTTP del Frontend pasan por Api.solicitar().
   - Agrega la URL base (config.js), el Bearer token y los encabezados JSON.
   - Interpreta las respuestas uniformes { statusCode, message, data, errors }.
   - Rechaza SIEMPRE con { status, mensaje, errores, cuerpo }:
       0 = sin conexión / tiempo agotado, 400, 401, 403, 404, 409, 429, 5xx.
   - 401 (salvo en rutas públicas como Login) => cierra la sesión local y
     redirige al login. 403 => mensaje de permisos, la sesión sigue activa.
   Requiere: config.js. Usa Auth (auth.js) si está cargado.
   ========================================================================== */
(function () {
  "use strict";

  const base = String(window.ELCUATE_CONFIG.API_URL || "").replace(/\/+$/, "");
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

  /** Aplana errors del Backend a { campo: [mensajes] }.
      Acepta { campo: ["msg"] } y el formato de ModelState { campo: { errors: [{ errorMessage }] } }. */
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

    if (status === 401 && !op.publico && window.Auth) window.Auth.sesionExpirada();

    throw { status, mensaje, errores, cuerpo };
  }

  /** GET que devuelve el arreglo `data`; un 404 de listado ("no se encontraron…") equivale a lista vacía. */
  async function lista(ruta, query) {
    try {
      const r = await solicitar("GET", ruta, { query });
      return (r && Array.isArray(r.data)) ? r.data : [];
    } catch (e) {
      if (e && e.status === 404) return [];
      throw e;
    }
  }

  window.Api = {
    solicitar,
    lista,
    get: (ruta, query) => solicitar("GET", ruta, { query }),
    post: (ruta, cuerpo, op) => solicitar("POST", ruta, Object.assign({ cuerpo }, op)),
    put: (ruta, cuerpo) => solicitar("PUT", ruta, { cuerpo }),
    del: (ruta) => solicitar("DELETE", ruta),
    /** Mensaje listo para mostrar al usuario a partir de cualquier error. */
    mensajeError: (err, porDefecto) => (err && err.mensaje) || porDefecto || mensajePorDefecto(500)
  };
})();
