/* ==========================================================================
   SESIÓN
   --------------------------------------------------------------------------
   El JWT que emite /api/Autenticador/Login solo lleva dos claims: el email
   (ClaimTypes.Name) y el idRol (ClaimTypes.Role). NO lleva idUsuario, pero
   Pedidos.idUsuario, Kardex.idUsuario y Auditorias.idUsuario son obligatorios,
   así que después del login se busca al usuario por email en GET Usuarios.
   ========================================================================== */
(function (App) {
  "use strict";

  var cfg = App.config;
  var CLAVE = "cuate.sesion";
  var URI_NOMBRE = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
  var URI_ROL = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  var URI_ROL_ALT = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role";

  var sesion = {};

  function leer() {
    var bruto = cfg.almacen.leer(CLAVE);
    if (!bruto) return null;
    try { return JSON.parse(bruto); } catch (e) { return null; }
  }
  function escribir(datos) { cfg.almacen.guardar(CLAVE, JSON.stringify(datos)); }

  /* Decodifica el payload del JWT (base64url → JSON, seguro con tildes). */
  sesion.decodificar = function (token) {
    try {
      var b64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
      while (b64.length % 4) b64 += "=";
      var bin = atob(b64);
      var utf8 = decodeURIComponent(Array.prototype.map.call(bin, function (c) {
        return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2);
      }).join(""));
      return JSON.parse(utf8);
    } catch (e) { return null; }
  };

  sesion.datos = function () { return leer(); };
  sesion.token = function () { var s = leer(); return s ? s.token : null; };

  sesion.expirada = function () {
    var s = leer();
    if (!s || !s.exp) return false;                 // sin "exp" no se puede saber: lo decide el backend (401)
    return s.exp * 1000 <= Date.now();
  };

  /* Guarda el token y completa idUsuario/nombre buscando por email. */
  sesion.iniciar = async function (token) {
    var p = sesion.decodificar(token) || {};
    var email = p[URI_NOMBRE] || p.unique_name || p.name || p.email || "";
    var rol = p[URI_ROL] || p[URI_ROL_ALT] || p.role || null;
    if (Array.isArray(rol)) rol = rol[0];

    escribir({ token: token, exp: p.exp || null, email: email, idRol: rol == null ? null : Number(rol),
               idUsuario: null, nombre: email || "Usuario" });

    try { await sesion.resolverUsuario(); }
    catch (e) { console.warn("No se pudo identificar al usuario tras el login:", e.message); }
  };

  /* Busca al usuario logueado en GET Usuarios (por email) y lo memoriza. */
  sesion.resolverUsuario = async function () {
    var s = leer();
    if (!s) throw new Error("No hay sesión activa.");
    if (s.idUsuario) return s;
    var lista = await App.api.entidad("usuarios").listar();
    var yo = lista.filter(function (u) { return String(u.email || "").toLowerCase() === String(s.email || "").toLowerCase(); })[0];
    if (!yo) throw new Error("No se encontró en la base de datos al usuario con el correo " + s.email + ".");
    s.idUsuario = yo.idUsuario;
    s.nombre = App.ui.nombreCompleto(yo) || s.email;
    s.idRol = yo.idRol;
    escribir(s);
    return s;
  };

  /* idUsuario garantizado (o error claro) — para Pedidos, Kardex y Auditorías. */
  sesion.idUsuario = async function () {
    var s = await sesion.resolverUsuario();
    if (!s.idUsuario) throw new Error("No se pudo determinar el usuario de la sesión.");
    return s.idUsuario;
  };

  /* Redirige al login. Las páginas están en /html/, el login en la raíz. */
  sesion.irALogin = function (motivo) {
    window.location.replace(cfg.RAIZ + "index.html" + (motivo ? "?motivo=" + encodeURIComponent(motivo) : ""));
  };

  sesion.cerrar = function () {
    cfg.almacen.borrar(CLAVE);
    sesion.irALogin();
  };

  /* Llamado por api.js ante un 401: la sesión ya no sirve. */
  sesion.expulsar = function () {
    cfg.almacen.borrar(CLAVE);
    sesion.irALogin("expirada");
  };

  /* Se usa al inicio de cada pantalla interna. Devuelve false si redirige. */
  sesion.proteger = function () {
    if (!sesion.token()) { sesion.irALogin(); return false; }
    if (sesion.expirada()) { cfg.almacen.borrar(CLAVE); sesion.irALogin("expirada"); return false; }
    return true;
  };

  /* bfcache: al volver con "atrás" tras cerrar sesión, el navegador restaura la
     página sin ejecutar nada; se vuelve a comprobar la sesión. */
  window.addEventListener("pageshow", function (ev) {
    if (ev.persisted && document.body && document.body.dataset.publica !== "1") sesion.proteger();
  });

  App.sesion = sesion;
})(window.App = window.App || {});
