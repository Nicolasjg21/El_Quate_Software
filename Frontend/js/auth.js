/* ==========================================================================
   AUTH.JS — Sesión, token, guardia de páginas, permisos y cierre de sesión
   --------------------------------------------------------------------------
   Almacenamiento del token (Bearer JWT): SIEMPRE sessionStorage (muere con la
   pestaña). Nunca se usa localStorage; si quedó algo de versiones anteriores
   (opción "Recordar este dispositivo", ya retirada) se borra al cargar la página.
   El Backend autentica solo con el encabezado Authorization: Bearer (no usa
   cookies), por lo que el token debe ser legible por JavaScript. Mitigaciones:
   el token expira (Jwt:ExpirationMinutes), Logout lo revoca en el servidor, el
   Backend responde con CSP restrictivo y todo texto dinámico se escapa con esc().
   Nunca se escribe el token ni la contraseña en consola.

   Qué se guarda en el navegador (y nada más):
     elcuate_token    -> el JWT (necesario para el encabezado Authorization)
     elcuate_permisos -> { idRol, nombres[] } solo para pintar el menú sin parpadeo;
                         se vuelve a pedir al Backend en cada página (no es fuente
                         de verdad: el Backend responde 403 si no hay permiso).
   La contraseña, el correo y la fecha de expiración NO se guardan aparte
   (la expiración se lee del propio token: claim "exp").

   Requiere: config.js y api-cliente.js (cargados antes en <head>).
   ========================================================================== */
(function () {
  "use strict";

  const K_TOKEN = "elcuate_token";
  const K_EXPIRA = "elcuate_expira";   // ya no se escribe; solo se borra si quedó de versiones anteriores
  const K_PERMISOS = "elcuate_permisos";
  const PAGINA_LOGIN = "login.html";
  const PAGINA_ADMIN = "dashboard.html";

  /* Permiso requerido por página (nombres de PermisosSistema.cs en el Backend). */
  const PERMISO_PAGINA = {
    "dashboard.html": "auditorias.consultar",   // Panel Principal: solo administración
    "analiticas.html": "auditorias.consultar",  // Ventas y Analíticas: solo administración
    "usuarios.html": "usuarios.gestionar",
    "auditoria.html": "auditorias.consultar"
  };
  const PAGINA_OPERATIVA = "mesas.html";   // inicio de Caja y Mesero

  const pagina = (location.pathname.split("/").pop() || "").toLowerCase();
  const enLogin = pagina === PAGINA_LOGIN;

  /* ---------- Almacenamiento seguro (puede lanzar en modo privado) ---------- */
  /* Restos de versiones anteriores en localStorage (token, sesión y datos de prueba que antes
     se guardaban en el navegador): ninguno se usa ya y se eliminan siempre al cargar. */
  const CLAVES_ANTIGUAS = ["token", "cuate.sesion", "nombreUsuario"];
  try {
    Object.keys(localStorage).forEach((k) => {
      if (/^elcuate_/.test(k) || CLAVES_ANTIGUAS.indexOf(k) !== -1) localStorage.removeItem(k);
    });
  } catch (e) { /* sin almacenamiento */ }

  function leer(clave) {
    try { return sessionStorage.getItem(clave); } catch (e) { return null; }
  }
  function dondeEsta(clave) {
    try {
      if (sessionStorage.getItem(clave) !== null) return sessionStorage;
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

  /** Guarda (o reemplaza) el token, siempre en sessionStorage. */
  function guardarToken(nuevo) {
    if (!decodificar(nuevo)) return false;   // no se guarda nada que no sea un JWT legible
    let destino;
    try { destino = sessionStorage; } catch (e) { destino = null; }
    if (!destino) return false;
    try {
      destino.setItem(K_TOKEN, nuevo);
      destino.removeItem(K_EXPIRA);   // "expiraEn" no se persiste: el token ya trae "exp"
      programarExpiracion();
      return true;
    } catch (e) { return false; }
  }

  /** Sustituye el token anterior (p. ej. tras CambiarPassword). El viejo deja de usarse de inmediato. */
  function reemplazarToken(nuevo) {
    return guardarToken(nuevo);
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

  /** forzar=true ignora la caché y consulta al Backend (la caché solo evita el parpadeo inicial;
      así un permiso retirado o una caché editada a mano no sobreviven más allá de una página). */
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
      /* Sin respuesta del Backend: se conserva la caché si existía (el Backend sigue validando cada acción). */
      if (cache) { permisos = cache; permisosCargados = true; }
      else { permisos = []; permisosCargados = false; }
    }
    return permisos;
  }

  function tiene(nombrePermiso) { return permisos.indexOf(nombrePermiso) !== -1; }

  /** Administración = rol con permisos de seguridad (solo el administrador los tiene). */
  function esAdmin() { return tiene("seguridad.gestionar"); }

  /** Página de inicio según el rol: administración -> Panel Principal; Caja y Mesero -> Mesas. */
  function paginaInicio() {
    if (!permisosCargados) {
      const cache = leerCachePermisos();
      if (cache) { permisos = cache; permisosCargados = true; }
    }
    return permisosCargados && tiene("auditorias.consultar") ? PAGINA_ADMIN : PAGINA_OPERATIVA;
  }

  /* ---------- Login / Logout ---------- */
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
  async function logout() {
    if (cerrando) return;   // evita doble clic: una sola auditoría y una sola revocación
    cerrando = true;
    expirando = true; // un 401 al revocar no debe mostrarse como "sesión expirada"
    const u = usuario();
    if (u) await window.Api.auditar("Usuarios", "LOGOUT", null, { Usuario: u.email, idRol: u.idRol });
    try { if (token()) await window.Api.post("api/Autenticador/Logout"); } catch (e) { /* aun así se cierra localmente */ }
    borrarSesionLocal();
    irALogin();
  }

  /* sessionStorage es propio de cada pestaña. Si otra pestaña con el mismo token cierra
     sesión, el Backend lo revoca y esta pestaña sale al recibir el 401 de su siguiente llamada. */

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

    await cargarPermisos(true);

    /* Opciones de menú y páginas según permisos reales del rol */
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
        '<label for="cp-actual">Contraseña actual</label><input type="password" id="cp-actual" maxlength="128" autocomplete="new-password" readonly>' +
        '<label for="cp-nueva">Nueva contraseña</label><input type="password" id="cp-nueva" maxlength="128" autocomplete="new-password" readonly>' +
        '<label for="cp-confirmar">Confirmar nueva contraseña</label><input type="password" id="cp-confirmar" maxlength="128" autocomplete="new-password" readonly>' +
        '<div class="cp-err" id="cp-error" role="alert"></div>' +
        '<div class="cp-botones"><button type="button" id="cp-cancelar">Cancelar</button><button type="button" class="cp-ok" id="cp-guardar">Guardar</button></div></div>';
      document.body.appendChild(ov);
      ov.addEventListener("click", (e) => { if (e.target === ov) ov.classList.remove("cp-visible"); });
      document.getElementById("cp-cancelar").addEventListener("click", () => ov.classList.remove("cp-visible"));
      document.getElementById("cp-guardar").addEventListener("click", guardarPassword);
    }
    /* El navegador puede autocompletar la contraseña guardada del inicio de sesión: los campos
       arrancan en solo lectura (se habilitan al enfocarlos) y se vacían de nuevo tras abrir. */
    const campos = ["cp-actual", "cp-nueva", "cp-confirmar"].map((id) => document.getElementById(id));
    const vaciar = () => campos.forEach((c) => { if (!c.dataset.escrito) c.value = ""; });
    campos.forEach((c) => {
      c.readOnly = true;
      delete c.dataset.escrito;
      if (!c.dataset.listo) {
        c.dataset.listo = "1";
        c.addEventListener("focus", () => { c.readOnly = false; });
        c.addEventListener("keydown", () => { c.dataset.escrito = "1"; });   // lo escribió la persona
      }
    });
    vaciar();
    document.getElementById("cp-error").textContent = "";
    ov.classList.add("cp-visible");
    setTimeout(vaciar, 150);
    setTimeout(vaciar, 600);
    campos[0].readOnly = false;
    campos[0].focus();
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
        reemplazarToken(r.token);
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
    if (sesionValida()) location.replace(paginaInicio());
  } else if (!sesionValida()) {
    const habiaToken = !!leer(K_TOKEN);
    borrarSesionLocal();
    irALogin(habiaToken ? "expirada" : "");
  } else {
    programarExpiracion();

    /* Permisos en caché (se guardan al iniciar sesión): permiten decidir antes de pintar. */
    const cache = leerCachePermisos();
    if (cache) { permisos = cache; permisosCargados = true; }
    const requerido = PERMISO_PAGINA[pagina];
    let oculta = false;
    if (requerido) {
      if (permisosCargados && !tiene(requerido)) {
        location.replace(paginaInicio());
      } else if (!permisosCargados) {
        document.documentElement.style.visibility = "hidden";   // se muestra al confirmar el permiso
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

  window.Auth = {
    token, usuario, sesionValida, login, logout, sesionExpirada,
    reemplazarToken, tiene, esAdmin, paginaInicio, listo,
    get permisosCargados() { return permisosCargados; },
    mostrarAviso
  };
})();
