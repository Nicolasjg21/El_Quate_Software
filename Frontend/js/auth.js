/**
 * @file auth.js
 * @description Sesión, autorización en la interfaz y cierre de sesión.
 *   - Guarda el JWT exclusivamente en sessionStorage (la sesión termina al cerrar la pestaña).
 *   - Protege cada página según los permisos del rol y oculta las opciones de menú no autorizadas.
 *   El cambio de contraseña se realiza en la página Configuración (configuracion.js).
 * @requires config.js, api-cliente.js
 * @exports window.Auth
 *
 * Almacenamiento en el navegador:
 *   elcuate_token     JWT necesario para el encabezado Authorization.
 *   elcuate_permisos  { idRol, nombres[] } para pintar el menú sin esperar a la API.
 *                     No es fuente de verdad: se vuelve a consultar en cada página y el Backend valida cada acción.
 *
 * Endpoints: api/Autenticador/Login, api/Autenticador/Logout, api/RolesPermisos/GetRolesPermisos,
 *            api/Permisos/GetPermisos, api/Usuarios/GetUsuariosById/{id}
 */

(function () {
  "use strict";

  /** Claves de almacenamiento. */
  const K_TOKEN = "elcuate_token";
  const K_EXPIRA = "elcuate_expira";
  const K_PERMISOS = "elcuate_permisos";
  const PAGINA_LOGIN = "login.html";
  const PAGINA_ADMIN = "dashboard.html";

  /** Permiso requerido por cada página. Las páginas no listadas solo exigen sesión activa. */
  const PERMISO_PAGINA = {
    "dashboard.html": "auditorias.consultar",
    "analiticas.html": "auditorias.consultar",
    "usuarios.html": "usuarios.gestionar",
    "auditoria.html": "auditorias.consultar"
  };

  /** Página de inicio de los roles operativos (caja y mesero). */
  const PAGINA_OPERATIVA = "mesas.html";

  const pagina = (location.pathname.split("/").pop() || "").toLowerCase();
  const enLogin = pagina === PAGINA_LOGIN;

  /** Claves obsoletas en localStorage que se eliminan al cargar cualquier página. */
  const CLAVES_ANTIGUAS = ["token", "cuate.sesion", "nombreUsuario"];
  try {
    Object.keys(localStorage).forEach((k) => {
      if (/^elcuate_/.test(k) || CLAVES_ANTIGUAS.indexOf(k) !== -1) localStorage.removeItem(k);
    });
  } catch (e) { }

  /* ---------- Almacenamiento ---------- */
  function leer(clave) {
    try { return sessionStorage.getItem(clave); } catch (e) { return null; }
  }
  function dondeEsta(clave) {
    try {
      if (sessionStorage.getItem(clave) !== null) return sessionStorage;
    } catch (e) { }
    return null;
  }

  /** Elimina el token y la caché de permisos. */
  function borrarSesionLocal() {
    [K_TOKEN, K_EXPIRA, K_PERMISOS].forEach((k) => {
      try { sessionStorage.removeItem(k); } catch (e) {}
      try { localStorage.removeItem(k); } catch (e) {}
    });
  }

  /* ---------- Token ---------- */

  /**
   * Decodifica el payload de un JWT. Solo se usa para leer datos en la interfaz;
   * la firma la valida el Backend.
   * @param {string} token
   * @returns {object|null}
   */
  function decodificar(token) {
    try {
      const b64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
      const json = decodeURIComponent(atob(b64).split("").map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2)).join(""));
      return JSON.parse(json);
    } catch (e) { return null; }
  }

  /** Devuelve el primer claim presente entre varios nombres posibles (cortos o URI de .NET). */
  function claim(payload, nombres) {
    for (const n of nombres) if (payload && payload[n] !== undefined) return payload[n];
    return undefined;
  }

  /**
   * Token vigente.
   * @returns {string|null} null si no existe o ya venció.
   */
  function token() {
    const t = leer(K_TOKEN);
    if (!t) return null;
    const p = decodificar(t);
    if (!p) return null;
    if (typeof p.exp === "number" && p.exp * 1000 <= Date.now()) return null;
    return t;
  }

  /**
   * Datos del usuario autenticado, tomados del token.
   * @returns {{idUsuario:number, idRol:number, email:string}|null}
   */
  function usuario() {
    const t = token();
    const p = t && decodificar(t);
    if (!p) return null;
    const id = Number(claim(p, ["nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "sub"]));
    const rol = Number(claim(p, ["role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]));
    const email = claim(p, ["unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "name", "email"]);
    return { idUsuario: id, idRol: rol, email: email || "" };
  }

  function sesionValida() { return !!token(); }

  /**
   * Guarda el token en sessionStorage y programa el cierre por vencimiento.
   * @param {string} nuevo JWT.
   * @returns {boolean} false si el token no es legible o el navegador bloquea el almacenamiento.
   */
  function guardarToken(nuevo) {
    if (!decodificar(nuevo)) return false;
    let destino;
    try { destino = sessionStorage; } catch (e) { destino = null; }
    if (!destino) return false;
    try {
      destino.setItem(K_TOKEN, nuevo);
      destino.removeItem(K_EXPIRA);
      programarExpiracion();
      return true;
    } catch (e) { return false; }
  }

  /** Sustituye el token actual; se usa tras CambiarPassword, que invalida el anterior. */
  function reemplazarToken(nuevo) {
    return guardarToken(nuevo);
  }

  /* ---------- Vencimiento de la sesión ---------- */
  let temporizadorExpira = null;
  function programarExpiracion() {
    clearTimeout(temporizadorExpira);
    const t = leer(K_TOKEN);
    const p = t && decodificar(t);
    if (!p || typeof p.exp !== "number") return;
    const ms = p.exp * 1000 - Date.now();
    if (ms <= 0) return;
    temporizadorExpira = setTimeout(sesionExpirada, Math.min(ms + 500, 2147483000));
  }

  function irALogin(motivo) {
    location.replace(PAGINA_LOGIN + (motivo ? "?motivo=" + motivo : ""));
  }

  let expirando = false;

  /** Cierra la sesión local y redirige al inicio de sesión. */
  function sesionExpirada() {
    if (expirando) return;
    expirando = true;
    borrarSesionLocal();
    if (!enLogin) irALogin("expirada");
  }

  /* ---------- Permisos del rol ---------- */
  let permisos = [];
  let permisosCargados = false;

  /** Permisos en caché, solo si pertenecen al rol del token actual. */
  function leerCachePermisos() {
    try {
      const c = JSON.parse(leer(K_PERMISOS) || "null");
      const u = usuario();
      if (c && u && c.idRol === u.idRol && Array.isArray(c.nombres)) return c.nombres;
    } catch (e) {}
    return null;
  }

  /**
   * Obtiene los nombres de permiso del rol actual (RolesPermisos + Permisos).
   * @param {boolean} [forzar] true para ignorar la caché y consultar la API.
   * @returns {Promise<string[]>}
   */
  async function cargarPermisos(forzar) {
    const u = usuario();
    if (!u) { permisos = []; return permisos; }
    const cache = leerCachePermisos();
    if (cache && !forzar) { permisos = cache; permisosCargados = true; return permisos; }
    try {
      const [rp, todos] = await Promise.all([
        window.Api.lista("api/RolesPermisos/GetRolesPermisos"),
        window.Api.lista("api/Permisos/GetPermisos")
      ]);
      const ids = new Set(rp.filter((x) => x.idRol === u.idRol).map((x) => x.idPermiso));
      permisos = todos.filter((p) => ids.has(p.idPermiso)).map((p) => p.nombrePermiso);
      permisosCargados = true;
      const destino = dondeEsta(K_TOKEN);
      if (destino) { try { destino.setItem(K_PERMISOS, JSON.stringify({ idRol: u.idRol, nombres: permisos })); } catch (e) {} }
    } catch (e) {
      if (cache) { permisos = cache; permisosCargados = true; }
      else { permisos = []; permisosCargados = false; }
    }
    return permisos;
  }

  /**
   * @param {string} nombrePermiso Nombre según PermisosSistema del Backend.
   * @returns {boolean}
   */
  function tiene(nombrePermiso) { return permisos.indexOf(nombrePermiso) !== -1; }

  /** Un rol administrativo es el que tiene "seguridad.gestionar". */
  function esAdmin() { return tiene("seguridad.gestionar"); }

  /** Página de inicio según el rol: Panel Principal para administración, Mesas para la operación. */
  function paginaInicio() {
    if (!permisosCargados) {
      const cache = leerCachePermisos();
      if (cache) { permisos = cache; permisosCargados = true; }
    }
    return permisosCargados && tiene("auditorias.consultar") ? PAGINA_ADMIN : PAGINA_OPERATIVA;
  }

  /* ---------- Inicio y cierre de sesión ---------- */

  /**
   * Inicia sesión, guarda el token, carga los permisos y registra el evento LOGIN.
   * @param {string} email
   * @param {string} password
   * @returns {Promise<object>} Respuesta del Backend.
   */
  async function login(email, password) {
    const r = await window.Api.post("api/Autenticador/Login", { email, password }, { publico: true });
    if (!r || !r.token) throw { status: 500, mensaje: "Respuesta de inicio de sesión inválida.", errores: {} };
    borrarSesionLocal();
    if (!guardarToken(r.token)) {
      throw { status: 0, mensaje: "El navegador no permite guardar la sesión. Habilite el almacenamiento del sitio.", errores: {} };
    }
    await cargarPermisos();
    const u = usuario();
    await window.Api.auditar("Usuarios", "LOGIN", null, { Usuario: email, idRol: u ? u.idRol : undefined });
    return r;
  }

  let cerrando = false;

  /**
   * Registra LOGOUT, revoca el token en el Backend y limpia la sesión local.
   * Ignora clics repetidos mientras el cierre está en curso.
   */
  async function logout() {
    if (cerrando) return;
    cerrando = true;
    expirando = true;
    const u = usuario();
    if (u) await window.Api.auditar("Usuarios", "LOGOUT", null, { Usuario: u.email, idRol: u.idRol });
    try { if (token()) await window.Api.post("api/Autenticador/Logout"); } catch (e) { }
    borrarSesionLocal();
    irALogin();
  }

  /* ---------- Interfaz común ---------- */

  /**
   * Muestra un aviso temporal. Usa el elemento #toast de la página o crea uno propio.
   * @param {string} mensaje
   * @param {boolean} [error]
   */
  function mostrarAviso(mensaje, error) {
    let t = document.getElementById("toast");
    if (!t) {
      t = document.createElement("div");
      t.id = "toast";
      t.setAttribute("role", "status");
      t.style.cssText = "position:fixed;left:50%;bottom:28px;transform:translateX(-50%);background:#313638;color:#fff;padding:12px 20px;border-radius:10px;z-index:3000;font:500 14px system-ui,sans-serif;display:none";
      document.body.appendChild(t);
    }
    if (!t.classList.contains("toast")) {
      t.style.background = error ? "#D8432B" : "#313638";
      t.style.display = "block";
      clearTimeout(mostrarAviso._t);
      mostrarAviso._t = setTimeout(() => { t.style.display = "none"; }, 3500);
      t.textContent = mensaje;
      return;
    }
    t.textContent = mensaje;
    t.classList.toggle("toast--error", !!error);
    t.classList.add("toast--visible");
    clearTimeout(mostrarAviso._t);
    mostrarAviso._t = setTimeout(() => t.classList.remove("toast--visible"), 3500);
  }

  /** Conecta el cierre de sesión del menú, muestra el nombre del usuario y aplica los permisos. */
  async function prepararPagina() {
    const u = usuario();

    document.querySelectorAll('.sidebar a[href$="login.html"]').forEach((a) => {
      a.addEventListener("click", (e) => { e.preventDefault(); logout(); });
    });

    const fecha = document.querySelector(".header-usuario-fecha");
    if (fecha && !fecha.id) {
      const d = new Date();
      fecha.textContent = d.getDate() + "/" + (d.getMonth() + 1) + "/" + d.getFullYear();
    }

    const nombre = document.querySelector(".header-usuario-nombre");
    if (nombre && u) {
      nombre.textContent = u.email || "Usuario";
      window.Api.get("api/Usuarios/GetUsuariosById/" + u.idUsuario).then((r) => {
        const d = r && r.data;
        if (d && d.nombres) nombre.textContent = (d.nombres + " " + (d.apellidos || "")).trim();
      }).catch(() => { });
    }

    await cargarPermisos(true);

    if (permisosCargados) {
      Object.keys(PERMISO_PAGINA).forEach((pg) => {
        if (!tiene(PERMISO_PAGINA[pg])) {
          document.querySelectorAll('.sidebar a[href$="' + pg + '"]').forEach((a) => a.remove());
        }
      });
      const req = PERMISO_PAGINA[pagina];
      if (req && !tiene(req)) location.replace(paginaInicio());
    }
  }

  /* ---------- Protección inmediata de la página ----------
     Se ejecuta al cargar el <head>, antes de pintar. Si el permiso aún no se conoce,
     la página permanece oculta hasta confirmarlo. */
  let resolverListo;
  const listo = new Promise((r) => { resolverListo = r; });

  if (enLogin) {
    if (sesionValida()) location.replace(paginaInicio());
  } else if (!sesionValida()) {
    const habiaToken = !!leer(K_TOKEN);
    borrarSesionLocal();
    irALogin(habiaToken ? "expirada" : "");
  } else {
    programarExpiracion();

    const cache = leerCachePermisos();
    if (cache) { permisos = cache; permisosCargados = true; }
    const requerido = PERMISO_PAGINA[pagina];
    let oculta = false;
    if (requerido) {
      if (permisosCargados && !tiene(requerido)) {
        location.replace(paginaInicio());
      } else if (!permisosCargados) {
        document.documentElement.style.visibility = "hidden";
        oculta = true;
      }
    }

    document.addEventListener("DOMContentLoaded", () => {
      prepararPagina().finally(() => {
        if (oculta) document.documentElement.style.visibility = "";
        resolverListo();
      });
    });
  }

  /** API pública de sesión y permisos. "listo" se resuelve cuando la página ya aplicó los permisos. */
  window.Auth = {
    token, usuario, sesionValida, login, logout, sesionExpirada,
    reemplazarToken, tiene, esAdmin, paginaInicio, listo,
    get permisosCargados() { return permisosCargados; },
    mostrarAviso
  };
})();
