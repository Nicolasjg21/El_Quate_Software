/* ==========================================================================
   API — cliente HTTP (fetch)
   --------------------------------------------------------------------------
   Contrato REAL del backend (verificado en los controllers):
     • Todo excepto /Autenticador/Login exige  Authorization: Bearer <jwt>.
     • Respuestas OK:   { statusCode, message, data }   ← hay que desenvolver "data".
     • Lista vacía:     HTTP 404 con JSON { statusCode:404, message }  (NO es un error).
     • Ruta inexistente: HTTP 404 SIN cuerpo (sí es un error de integración).
     • POST de Cuentas/DetallePedidos/Comprobantes/Compras responde 200 sin "data"
       (no devuelve el id creado); Productos/Pedidos/Usuarios responden 201 con "data".
     • Validación fallida: 400 con { errors: { campo: ["mensaje"] } }.
   ========================================================================== */
(function (App) {
  "use strict";

  var cfg = App.config;
  var api = {};
  var TIEMPO_MAX_MS = 90000;

  function crearError(status, mensaje, cuerpo, metodo, ruta) {
    var e = new Error(mensaje);
    e.name = "ApiError";
    e.status = status;
    e.cuerpo = cuerpo || null;
    e.metodo = metodo;
    e.ruta = ruta;
    return e;
  }

  /* Convierte cualquier forma de error del backend en un texto legible. */
  function extraerMensaje(status, json, texto, metodo, ruta) {
    if (typeof json === "string" && json) return json;                       // Login: Unauthorized("texto")
    if (json && typeof json === "object") {
      var partes = [];
      var errs = json.errors;
      if (errs && typeof errs === "object") {
        Object.keys(errs).forEach(function (campo) {
          var v = errs[campo], msgs = [];
          if (Array.isArray(v)) msgs = v.map(String);
          else if (v && Array.isArray(v.errors)) msgs = v.errors.map(function (x) { return x.errorMessage || String(x); });
          if (msgs.length) partes.push(campo + ": " + msgs.join(", "));
        });
      }
      var base = json.message || json.mensaje || json.title || "";
      var detalle = json.detalle ? " (" + json.detalle + ")" : "";
      if (partes.length) return (base ? base + " " : "") + "[" + partes.join(" | ") + "]";
      if (base) return base + detalle;
    }
    if (status === 404) return "La ruta " + metodo + " " + ruta + " no existe en el backend (404 sin cuerpo). Revisa js/core/config.js contra el controller.";
    if (status === 405) return "El backend no permite " + metodo + " en " + ruta + " (405).";
    if (status === 403) return "No tienes permiso para esta operación (403).";
    if (status === 415) return "El backend rechazó el formato de la petición (415).";
    if (status >= 500) return "Error interno del servidor (" + status + ").";
    return "Error " + status + (texto ? ": " + String(texto).slice(0, 200) : ".");
  }

  /* Petición base. opciones.sinAuth = true → no manda token ni expulsa en 401 (login). */
  async function pedir(metodo, ruta, cuerpo, opciones) {
    opciones = opciones || {};
    var url = cfg.API_URL + ruta;
    var cabeceras = { "Accept": "application/json" };
    var token = App.sesion.token();
    if (token && !opciones.sinAuth) cabeceras["Authorization"] = "Bearer " + token;
    if (cuerpo !== undefined) cabeceras["Content-Type"] = "application/json";

    var control = new AbortController();
    var reloj = setTimeout(function () { control.abort(); }, TIEMPO_MAX_MS);
    var resp;
    try {
      resp = await fetch(url, {
        method: metodo,
        headers: cabeceras,
        body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
        signal: control.signal
      });
    } catch (err) {
      var causa = err && err.name === "AbortError" ? "El servidor tardó más de " + (TIEMPO_MAX_MS / 1000) + " s en responder." :
        "No se pudo conectar con " + cfg.API_URL + ". Comprueba que el backend esté corriendo, que la URL " +
        "(Configuración) sea correcta y que el navegador confíe en el certificado HTTPS de desarrollo " +
        "(dotnet dev-certs https --trust).";
      throw crearError(0, causa, null, metodo, ruta);
    } finally {
      clearTimeout(reloj);
    }

    var texto = await resp.text();
    var json = null;
    if (texto) { try { json = JSON.parse(texto); } catch (e) { json = null; } }

    if (resp.status === 401 && !opciones.sinAuth) {
      App.sesion.expulsar();
      throw crearError(401, "La sesión expiró. Inicia sesión de nuevo.", json, metodo, ruta);
    }
    if (!resp.ok) throw crearError(resp.status, extraerMensaje(resp.status, json, texto, metodo, ruta), json, metodo, ruta);
    return json;
  }

  /* 404 "de negocio" = el backend respondió con su JSON { statusCode: 404 }. */
  function esNoEncontradoDeNegocio(err) {
    return err && err.status === 404 && err.cuerpo && typeof err.cuerpo === "object" && err.cuerpo.statusCode === 404;
  }

  /* Servicio CRUD para una entidad de config.js. */
  api.entidad = function (clave) {
    var e = cfg.ENTIDADES[clave];
    if (!e) throw new Error("Entidad desconocida: " + clave);
    var base = "/api/" + e.ctl + "/";
    return {
      clave: clave,
      pk: e.pk,
      listar: async function () {
        try {
          var r = await pedir("GET", base + e.listar);
          return r && Array.isArray(r.data) ? r.data : [];
        } catch (err) {
          if (esNoEncontradoDeNegocio(err)) return [];        // lista vacía, no error
          throw err;
        }
      },
      porId: async function (id) {
        try {
          var r = await pedir("GET", base + e.porId + "/" + encodeURIComponent(id));
          return r ? r.data : null;
        } catch (err) {
          if (esNoEncontradoDeNegocio(err)) return null;
          throw err;
        }
      },
      crear: async function (obj) {
        var r = await pedir("POST", base + e.crear, obj);
        return r && r.data !== undefined ? r.data : null;     // null cuando el backend no devuelve la entidad
      },
      editar: async function (obj) {
        var r = await pedir("PUT", base + e.editar, obj);
        return r && r.data !== undefined ? r.data : null;
      },
      eliminar: async function (id) {
        return pedir("DELETE", base + e.eliminar + "/" + encodeURIComponent(id));
      }
    };
  };

  /* Login: POST { email, password } → { token }. */
  api.login = async function (email, password) {
    var r = await pedir("POST", cfg.LOGIN, { email: email, password: password }, { sinAuth: true });
    var token = r && (r.token || r.Token);
    if (!token) throw crearError(200, "El backend respondió sin token.", r, "POST", cfg.LOGIN);
    return token;
  };

  /* Traduce errores frecuentes a una causa accionable (para el usuario y para el profesor). */
  api.explicar = function (err, contexto) {
    var m = err && err.message ? err.message : String(err);
    if (err && err.status === 400 && /field is required/i.test(m)) {
      return m + "  →  Causa probable: el backend valida como [Required] las propiedades de navegación " +
             "(categoria, rol, mesa…) porque el proyecto tiene <Nullable>enable</Nullable>. " +
             "Solución de 1 línea en Program.cs: AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true). Ver informe, hallazgo B1.";
    }
    if (err && (err.status === 500 || err.status === 400) && /(delete|eliminar)/i.test(String(contexto || "")) ) {
      return m + "  →  Es probable que el registro esté relacionado con otros datos (llave foránea) y no pueda eliminarse.";
    }
    return m;
  };

  /* Auditoría desde el frontend (Auditorias no la escribe ningún controller).
     Nunca lanza: una auditoría fallida no debe romper la operación principal. */
  api.auditar = async function (tabla, accion, antes, despues) {
    if (!cfg.OPCIONES.AUDITORIA_DESDE_FRONTEND) return;
    try {
      var idUsuario = await App.sesion.idUsuario();
      await api.entidad("auditorias").crear({
        tabla: String(tabla).slice(0, 50),
        accion: String(accion).slice(0, 50),
        idUsuario: idUsuario,
        fecha: App.ui.ahoraLocal(),
        datosAnteriores: antes ? JSON.stringify(antes) : null,
        datosNuevos: despues ? JSON.stringify(despues) : null
      });
    } catch (e) {
      console.warn("Auditoría no registrada (" + tabla + "/" + accion + "):", e.message);
    }
  };

  /* Prueba de humo de un endpoint de lectura (para el diagnóstico). */
  api.probar = async function (clave) {
    var t0 = performance.now();
    try {
      var filas = await api.entidad(clave).listar();
      return { ok: true, filas: filas, ms: Math.round(performance.now() - t0) };
    } catch (e) {
      return { ok: false, error: e.message, status: e.status, ms: Math.round(performance.now() - t0) };
    }
  };

  api._pedir = pedir;   // solo para pruebas / diagnóstico
  App.api = api;
})(window.App = window.App || {});
