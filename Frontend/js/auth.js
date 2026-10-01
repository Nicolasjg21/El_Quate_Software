/* ==========================================================================
   AUTH.JS — Sesión, token, guardia de páginas, permisos y cierre de sesión
   --------------------------------------------------------------------------
   Almacenamiento del token (Bearer JWT):
     - "Recordar este dispositivo" marcado  -> localStorage  (persiste al cerrar el navegador)
     - sin marcar                           -> sessionStorage (muere con la pestaña)
   El Backend autentica solo con el encabezado Authorization: Bearer (no usa
   cookies), por lo que el token debe ser legible por JavaScript. Mitigaciones:
   el token expira (Jwt:ExpirationMinutes), Logout lo revoca en el servidor, el
   Backend responde con CSP restrictivo y todo texto dinámico se escapa con esc().
   Nunca se escribe el token ni la contraseña en consola.

   Requiere: config.js y api-cliente.js (cargados antes en <head>).
   ========================================================================== */
(function () {
  "use strict";

  const K_TOKEN = "elcuate_token";
  const K_EXPIRA = "elcuate_expira";
  const K_PERMISOS = "elcuate_permisos";
  const PAGINA_LOGIN = "login.html";
  const PAGINA_INICIO = "dashboard.html";

  /* Permiso requerido por página (nombres de PermisosSistema.cs en el Backend). */
  const PERMISO_PAGINA = {
    "usuarios.html": "usuarios.gestionar",
    "auditoria.html": "auditorias.consultar"
  };

  const pagina = (location.pathname.split("/").pop() || "").toLowerCase();
  const enLogin = pagina === PAGINA_LOGIN;

  /* ---------- Almacenamiento seguro (puede lanzar en modo privado) ---------- */
  function leer(clave) {
    try { return sessionStorage.getItem(clave) || localStorage.getItem(clave); } catch (e) { return null; }
  }
  function dondeEsta(clave) {
    try {
      if (sessionStorage.getItem(clave) !== null) return sessionStorage;
      if (localStorage.getItem(clave) !== null) return localStorage;
    } catch (e) { /* sin almacenamiento */ }
    return null;
  }
  function borrarSesionLocal() {
    [K_TOKEN, K_EXPIRA, K_PERMISOS].forEach((k) => {
      try { sessionStorage.removeItem(k); } catch (e) {}
      try { localStorage.removeItem(k); } catch (e) {}
    });
  }

  /* ---------- JWT (solo lectura de claims para la interfaz; el Backend es quien valida) ---------- */
  function decodificar(token) {
    try {
      const b64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
      const json = decodeURIComponent(atob(b64).split("").map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2)).join(""));
      return JSON.parse(json);
    } catch (e) { return null; }
  }
  function claim(payload, nombres) {
    for (const n of nombres) if (payload && payload[n] !== undefined) return payload[n];
    return undefined;
  }

  function token() {
    const t = leer(K_TOKEN);
    if (!t) return null;
    const p = decodificar(t);
    if (!p) return null;
    if (typeof p.exp === "number" && p.exp * 1000 <= Date.now()) return null;
    return t;
  }

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

  /** Guarda (o reemplaza) el token. Si ya había uno, se conserva el mismo almacenamiento. */
  function guardarToken(nuevo, expiraEn, recordar) {
    const previo = dondeEsta(K_TOKEN);
    let destino = previo;
    if (!destino) {
      try { destino = recordar ? localStorage : sessionStorage; } catch (e) { destino = null; }
    }
    if (!destino) return false;
    try {
      destino.setItem(K_TOKEN, nuevo);
      if (expiraEn) destino.setItem(K_EXPIRA, String(expiraEn)); else destino.removeItem(K_EXPIRA);
      programarExpiracion();
      return true;
    } catch (e) { return false; }
  }

  /** Sustituye el token anterior (p. ej. tras CambiarPassword). El viejo deja de usarse de inmediato. */
  function reemplazarToken(nuevo, expiraEn) {
    return guardarToken(nuevo, expiraEn, false);
  }

  /* ---------- Expiración / 401 ---------- */
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
  function sesionExpirada() {
    if (expirando) return;
    expirando = true;
    borrarSesionLocal();
    if (!enLogin) irALogin("expirada");
  }

  /* ---------- Permisos (RolesPermisos del Backend) ---------- */
  let permisos = [];
  let permisosCargados = false;

  function leerCachePermisos() {
    try {
      const c = JSON.parse(leer(K_PERMISOS) || "null");
      const u = usuario();
      if (c && u && c.idRol === u.idRol && Array.isArray(c.nombres)) return c.nombres;
    } catch (e) {}
    return null;
  }

  async function cargarPermisos() {
    const u = usuario();
    if (!u) { permisos = []; return permisos; }
    const cache = leerCachePermisos();
    if (cache) { permisos = cache; permisosCargados = true; return permisos; }
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
      permisos = [];
      permisosCargados = false;
    }
    return permisos;
  }

  function tiene(nombrePermiso) { return permisos.indexOf(nombrePermiso) !== -1; }

  /* ---------- Login / Logout ---------- */
  async function login(email, password, recordar) {
    const r = await window.Api.post("api/Autenticador/Login", { email, password }, { publico: true });
    if (!r || !r.token) throw { status: 500, mensaje: "Respuesta de inicio de sesión inválida.", errores: {} };
    borrarSesionLocal();
    if (!guardarToken(r.token, r.expiraEn, recordar)) {
      throw { status: 0, mensaje: "El navegador no permite guardar la sesión. Habilite el almacenamiento del sitio.", errores: {} };
    }
    await cargarPermisos();
    return r;
  }

  async function logout() {
    expirando = true; // un 401 al revocar no debe mostrarse como "sesión expirada"
    try { if (token()) await window.Api.post("api/Autenticador/Logout"); } catch (e) { /* aun así se cierra localmente */ }
    borrarSesionLocal();
    irALogin();
  }

  /* ---------- Interfaz común (menú, usuario, Configuración) ---------- */
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

  async function prepararPagina() {
    const u = usuario();

    /* Cierre de sesión (enlace del pie del menú) */
    document.querySelectorAll('.sidebar a[href$="login.html"]').forEach((a) => {
      a.addEventListener("click", (e) => { e.preventDefault(); logout(); });
    });

    /* Configuración -> cambio de contraseña */
    document.querySelectorAll(".sidebar-pie a.nav-item").forEach((a) => {
      if (/configuraci/i.test(a.textContent)) {
        a.addEventListener("click", (e) => { e.preventDefault(); abrirCambioPassword(); });
      }
    });

    /* Fecha del encabezado en páginas que no la rellenan por su cuenta */
    const fecha = document.querySelector(".header-usuario-fecha");
    if (fecha && !fecha.id) {
      const d = new Date();
      fecha.textContent = d.getDate() + "/" + (d.getMonth() + 1) + "/" + d.getFullYear();
    }

    /* Nombre del usuario en el encabezado */
    const nombre = document.querySelector(".header-usuario-nombre");
    if (nombre && u) {
      nombre.textContent = u.email || "Usuario";
      window.Api.get("api/Usuarios/GetUsuariosById/" + u.idUsuario).then((r) => {
        const d = r && r.data;
        if (d && d.nombres) nombre.textContent = (d.nombres + " " + (d.apellidos || "")).trim();
      }).catch(() => { /* se queda el correo */ });
    }

    await cargarPermisos();

    /* Opciones de menú y páginas según permisos reales del rol */
    if (permisosCargados) {
      Object.keys(PERMISO_PAGINA).forEach((pg) => {
        if (!tiene(PERMISO_PAGINA[pg])) {
          document.querySelectorAll('.sidebar a[href$="' + pg + '"]').forEach((a) => a.remove());
        }
      });
      const req = PERMISO_PAGINA[pagina];
      if (req && !tiene(req)) location.replace(PAGINA_INICIO);
    }
  }

  /* ---------- Cambio de contraseña (PUT api/Usuarios/CambiarPassword/{id}) ---------- */
  function abrirCambioPassword() {
    const u = usuario();
    if (!u) return;
    let ov = document.getElementById("cp-overlay");
    if (!ov) {
      const css = document.createElement("style");
      css.textContent =
        "#cp-overlay{position:fixed;inset:0;background:rgba(0,0,0,.45);display:none;align-items:center;justify-content:center;z-index:2500;padding:16px}" +
        "#cp-overlay.cp-visible{display:flex}" +
        "#cp-caja{background:#fff;border-radius:14px;width:100%;max-width:400px;padding:24px;font-family:inherit;color:#1F2426}" +
        "#cp-caja h2{margin:0 0 4px;font-size:18px}#cp-caja p.cp-sub{margin:0 0 16px;color:#6B7280;font-size:13px}" +
        "#cp-caja label{display:block;font-size:13px;font-weight:600;margin:12px 0 4px}" +
        "#cp-caja input{width:100%;box-sizing:border-box;padding:10px 12px;border:1px solid #E3E5E6;border-radius:8px;font:inherit}" +
        "#cp-caja .cp-err{color:#D8432B;font-size:12px;min-height:16px;margin-top:4px}" +
        "#cp-caja .cp-botones{display:flex;gap:8px;justify-content:flex-end;margin-top:18px}" +
        "#cp-caja button{padding:10px 16px;border-radius:8px;border:1px solid #E3E5E6;background:#fff;font:600 14px inherit;cursor:pointer}" +
        "#cp-caja button.cp-ok{background:#F06543;border-color:#F06543;color:#fff}#cp-caja button:disabled{opacity:.6;cursor:wait}";
      document.head.appendChild(css);
      ov = document.createElement("div");
      ov.id = "cp-overlay";
      ov.innerHTML =
        '<div id="cp-caja" role="dialog" aria-modal="true" aria-labelledby="cp-titulo">' +
        '<h2 id="cp-titulo">Cambiar contraseña</h2><p class="cp-sub">La nueva contraseña debe tener entre 8 y 128 caracteres.</p>' +
        '<label for="cp-actual">Contraseña actual</label><input type="password" id="cp-actual" maxlength="128" autocomplete="current-password">' +
        '<label for="cp-nueva">Nueva contraseña</label><input type="password" id="cp-nueva" maxlength="128" autocomplete="new-password">' +
        '<label for="cp-confirmar">Confirmar nueva contraseña</label><input type="password" id="cp-confirmar" maxlength="128" autocomplete="new-password">' +
        '<div class="cp-err" id="cp-error" role="alert"></div>' +
        '<div class="cp-botones"><button type="button" id="cp-cancelar">Cancelar</button><button type="button" class="cp-ok" id="cp-guardar">Guardar</button></div></div>';
      document.body.appendChild(ov);
      ov.addEventListener("click", (e) => { if (e.target === ov) ov.classList.remove("cp-visible"); });
      document.getElementById("cp-cancelar").addEventListener("click", () => ov.classList.remove("cp-visible"));
      document.getElementById("cp-guardar").addEventListener("click", guardarPassword);
    }
    ["cp-actual", "cp-nueva", "cp-confirmar"].forEach((id) => { document.getElementById(id).value = ""; });
    document.getElementById("cp-error").textContent = "";
    ov.classList.add("cp-visible");
    document.getElementById("cp-actual").focus();
  }

  async function guardarPassword() {
    const u = usuario();
    const err = document.getElementById("cp-error");
    const btn = document.getElementById("cp-guardar");
    const actual = document.getElementById("cp-actual").value;
    const nueva = document.getElementById("cp-nueva").value;
    const conf = document.getElementById("cp-confirmar").value;
    err.textContent = "";

    if (!actual) { err.textContent = "La contraseña actual es obligatoria."; return; }
    if (nueva.length < 8 || nueva.length > 128) { err.textContent = "La nueva contraseña debe tener entre 8 y 128 caracteres."; return; }
    if (nueva !== conf) { err.textContent = "La nueva contraseña y la confirmación no coinciden."; return; }

    btn.disabled = true;
    try {
      const r = await window.Api.put("api/Usuarios/CambiarPassword/" + u.idUsuario,
        { passwordActual: actual, nuevaPassword: nueva, confirmarPassword: conf });
      /* El Backend invalida el token anterior y entrega uno nuevo: se reemplaza de inmediato. */
      if (r && r.token) {
        reemplazarToken(r.token, r.expiraEn);
      }
      document.getElementById("cp-overlay").classList.remove("cp-visible");
      mostrarAviso("Contraseña actualizada correctamente.");
    } catch (e) {
      if (e && e.status !== 401) err.textContent = window.Api.mensajeError(e);
    } finally {
      btn.disabled = false;
    }
  }

  /* ---------- Guardia inmediata (se ejecuta al cargar el <head>, antes de pintar) ---------- */
  let resolverListo;
  const listo = new Promise((r) => { resolverListo = r; });

  if (enLogin) {
    if (sesionValida()) location.replace(PAGINA_INICIO);
  } else if (!sesionValida()) {
    const habiaToken = !!leer(K_TOKEN);
    borrarSesionLocal();
    irALogin(habiaToken ? "expirada" : "");
  } else {
    programarExpiracion();
    document.addEventListener("DOMContentLoaded", () => {
      prepararPagina().finally(resolverListo);
    });
  }

  window.Auth = {
    token, usuario, sesionValida, login, logout, sesionExpirada,
    reemplazarToken, tiene, listo,
    get permisosCargados() { return permisosCargados; },
    mostrarAviso
  };
})();
