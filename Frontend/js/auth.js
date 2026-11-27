/**
 * @file auth.js
 * @description Sesión, autorización en la interfaz y cierre de sesión.
 *   - Guarda el JWT exclusivamente en sessionStorage (la sesión termina al cerrar la pestaña).
 *   - Protege cada página según los permisos del rol y oculta las opciones de menú no autorizadas.
 *   - Las páginas de PAGINAS_PUBLICAS (la ayuda) también se abren sin sesión.
 *   - Con una contraseña temporal (restablecida por la administración) la página queda bloqueada
 *     por una ventana que no se puede cerrar hasta definir la contraseña nueva.
 *   El cambio de contraseña se realiza en la página Configuración (configuracion.js).
 * @requires config.js, api-cliente.js
 * @exports window.Auth
 *
 * Almacenamiento en el navegador:
 *   elcuate_token     JWT necesario para el encabezado Authorization.
 *   elcuate_permisos  { idRol, nombres[] } para pintar el menú sin esperar a la API.
 *                     No es fuente de verdad: se vuelve a consultar en cada página y el Backend valida cada acción.
 *
 * Endpoints: api/Autenticador/Login, api/Autenticador/Logout, api/Autenticador/Renovar,
 *            api/RolesPermisos/GetRolesPermisos, api/Permisos/GetPermisos, api/Usuarios/GetUsuariosById/{id}
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

  /** Páginas que se pueden abrir sin sesión. La ayuda se ofrece también desde el inicio de sesión (RF15). */
  const PAGINAS_PUBLICAS = ["ayuda.html"];

  const pagina = (location.pathname.split("/").pop() || "").toLowerCase();
  const enLogin = pagina === PAGINA_LOGIN;
  const enPublica = PAGINAS_PUBLICAS.indexOf(pagina) !== -1;

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
    sesionEnTope = false;   // CambiarPassword abre una sesión nueva, con su propio tope.
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

  /* ---------- Renovación de la sesión mientras se usa el sistema ----------
     Cada clic o tecla cuenta como uso. Con uso, el token se renueva cuando ya tiene más de 5 minutos
     (o un tercio de su vida, si es más corto), así que siempre le quedan al menos 55 de sus 60 minutos.
     Sin uso, nadie lo renueva y vence solo: la sesión se cierra entre 55 y 60 minutos después del último uso. */
  const RENOVAR_DESPUES_MS = 5 * 60 * 1000;
  /** Mínimo entre dos revisiones de actividad, para no evaluar cada tecla. */
  const REVISAR_CADA_MS = 15 * 1000;
  let ultimaRevision = 0;
  let renovando = false;
  /** true cuando el servidor ya no alarga el token (tope de horas de la sesión): no se vuelve a pedir. */
  let sesionEnTope = false;

  /** Payload del token guardado, o null si no hay uno legible. */
  function datosToken() {
    const t = leer(K_TOKEN);
    const p = t && decodificar(t);
    return p && typeof p.exp === "number" ? p : null;
  }

  /** Pide un token nuevo. Un 401 ya cierra la sesión desde Api; otros errores se reintentan con el siguiente uso. */
  async function renovarSesion() {
    if (renovando || sesionEnTope || !token()) return;
    renovando = true;
    const expAnterior = (datosToken() || { exp: 0 }).exp;
    try {
      const r = await window.Api.post("api/Autenticador/Renovar");
      const p = r && r.token && decodificar(r.token);
      if (p && typeof p.exp === "number") {
        // Si el token nuevo no vence después que el anterior, la sesión llegó a su duración máxima.
        if (p.exp <= expAnterior) sesionEnTope = true;
        guardarToken(r.token);
      }
    } catch (e) {
      /* 401: sesión vencida o duración máxima alcanzada; Api ya llevó al login. */
    } finally {
      renovando = false;
    }
  }

  /** Con cada uso del sistema (como máximo cada 15 s) renueva el token si ya tiene la edad suficiente. */
  function registrarActividad() {
    const ahora = Date.now();
    if (renovando || sesionEnTope || ahora - ultimaRevision < REVISAR_CADA_MS) return;
    ultimaRevision = ahora;
    const p = datosToken();
    const emitido = p && (typeof p.nbf === "number" ? p.nbf : p.iat);
    if (typeof emitido !== "number" || p.exp * 1000 <= ahora) return;
    const vida = (p.exp - emitido) * 1000;
    const edad = ahora - emitido * 1000;
    if (edad >= Math.min(RENOVAR_DESPUES_MS, vida / 3)) renovarSesion();
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
    // Primera petición de la jornada: espera ampliada y reintento, porque la base de datos
    // puede estar reanudándose. Reintentar el login no tiene efectos secundarios.
    const r = await window.Api.post("api/Autenticador/Login", { email, password }, {
      publico: true, reintentable: true, timeout: window.ELCUATE_CONFIG.TIMEOUT_LOGIN_MS
    });
    if (!r || !r.token) throw { status: 500, mensaje: "Respuesta de inicio de sesión inválida.", errores: {} };
    borrarSesionLocal();
    if (!guardarToken(r.token)) {
      throw { status: 0, mensaje: "El navegador no permite guardar la sesión. Habilite el almacenamiento del sitio.", errores: {} };
    }
    const u = usuario();
    await Promise.all([
      cargarPermisos(),
      window.Api.auditar("Usuarios", "LOGIN", null, { Usuario: email, idRol: u ? u.idRol : undefined })
    ]);
    return r;
  }

  let cerrando = false;

  /**
   * Registra LOGOUT, revoca el token en el Backend y limpia la sesión local.
   * Ignora clics repetidos mientras el cierre está en curso.
   * @param {string} [motivo] Motivo que muestra la pantalla de inicio de sesión (p. ej. "password").
   */
  async function logout(motivo) {
    if (cerrando) return;
    cerrando = true;
    expirando = true;
    const u = usuario();
    if (u) await window.Api.auditar("Usuarios", "LOGOUT", null, { Usuario: u.email, idRol: u.idRol });
    try { if (token()) await window.Api.post("api/Autenticador/Logout"); } catch (e) { }
    borrarSesionLocal();
    irALogin(motivo);
  }

  /* ---------- Contraseña temporal: cambio obligatorio ----------
     Si la administración restableció la contraseña, el token trae "cambio": "true". La página no
     carga nada ("listo" no se resuelve) y una ventana que no se puede cerrar exige la contraseña
     nueva. El servidor también bloquea cualquier otra acción hasta el cambio (403). */
  const CLAIM_CAMBIO = "cambio";
  let ventanaCambio = null;

  /** @returns {boolean} true si la sesión tiene una contraseña temporal sin cambiar. */
  function cambioPendiente() {
    const t = token();
    const p = t && decodificar(t);
    return !!p && String(p[CLAIM_CAMBIO]) === "true";
  }

  /** Muestra la ventana de cambio obligatorio y deja inerte el resto de la página. */
  function mostrarCambioObligatorio() {
    if (ventanaCambio) return;
    const u = usuario();
    if (!u) { sesionExpirada(); return; }

    Array.from(document.body.children).forEach((el) => {
      el.inert = true;
      el.setAttribute("aria-hidden", "true");
    });
    document.documentElement.style.visibility = "";

    const estiloCampo = "width:100%;box-sizing:border-box;padding:10px 12px;border:1px solid #E0DFD5;border-radius:8px;font:inherit;font-size:14px;color:#313638;background:#fff";
    const estiloEtiqueta = "display:block;font-size:13px;font-weight:600;color:#313638;margin:12px 0 6px";
    ventanaCambio = document.createElement("div");
    ventanaCambio.id = "cambio-obligatorio";
    ventanaCambio.setAttribute("role", "dialog");
    ventanaCambio.setAttribute("aria-modal", "true");
    ventanaCambio.setAttribute("aria-labelledby", "co-titulo");
    ventanaCambio.style.cssText = "position:fixed;inset:0;z-index:6000;background:rgba(49,54,56,.9);display:flex;align-items:center;justify-content:center;padding:16px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif";
    ventanaCambio.innerHTML =
      '<form id="co-form" novalidate autocomplete="off" style="background:#fff;border-radius:14px;max-width:440px;width:100%;padding:28px 28px 24px;box-shadow:0 20px 50px rgba(0,0,0,.35);border-top:6px solid #F06543">' +
      '<h2 id="co-titulo" style="margin:0 0 6px;font-size:20px;color:#313638">Cambie su contraseña temporal</h2>' +
      '<p style="margin:0 0 4px;font-size:14px;line-height:1.5;color:#5c6366">La administración restableció su contraseña. Escriba la contraseña temporal que le entregaron y defina una nueva que solo usted conozca. Después deberá iniciar sesión con la contraseña nueva.</p>' +
      '<p id="co-usuario" style="margin:8px 0 0;font-size:13px;color:#313638;font-weight:600"></p>' +
      '<label for="co-actual" style="' + estiloEtiqueta + '">Contraseña temporal</label>' +
      '<input type="password" id="co-actual" maxlength="128" autocomplete="current-password" style="' + estiloCampo + '">' +
      '<label for="co-nueva" style="' + estiloEtiqueta + '">Nueva contraseña (entre 8 y 128 caracteres)</label>' +
      '<input type="password" id="co-nueva" maxlength="128" autocomplete="new-password" style="' + estiloCampo + '">' +
      '<label for="co-confirmar" style="' + estiloEtiqueta + '">Confirmar nueva contraseña</label>' +
      '<input type="password" id="co-confirmar" maxlength="128" autocomplete="new-password" style="' + estiloCampo + '">' +
      '<label style="display:flex;align-items:center;gap:8px;margin-top:12px;font-size:13px;color:#5c6366"><input type="checkbox" id="co-mostrar"> Mostrar contraseñas</label>' +
      '<p id="co-error" role="alert" hidden style="margin:14px 0 0;padding:10px 12px;border-radius:8px;background:#FDECE8;color:#B23A22;font-size:13px"></p>' +
      '<button type="submit" id="co-guardar" style="margin-top:18px;width:100%;padding:12px;border:0;border-radius:10px;background:#F06543;color:#fff;font:inherit;font-size:15px;font-weight:600;cursor:pointer">Guardar contraseña</button>' +
      '<button type="button" id="co-salir" style="margin-top:10px;width:100%;padding:10px;border:1px solid #E0DFD5;border-radius:10px;background:#fff;color:#313638;font:inherit;font-size:14px;cursor:pointer">Cerrar sesión</button>' +
      '</form>';
    document.body.appendChild(ventanaCambio);

    const el = (id) => ventanaCambio.querySelector("#" + id);
    el("co-usuario").textContent = "Usuario: " + (u.email || "");
    const campos = ["co-actual", "co-nueva", "co-confirmar"].map(el);
    const boton = el("co-guardar");
    const error = el("co-error");
    const mostrarError = (texto) => { error.textContent = texto; error.hidden = !texto; };
    let enviando = false;

    // La ventana no se cierra con Escape ni de ninguna otra forma.
    document.addEventListener("keydown", (e) => { if (e.key === "Escape") { e.preventDefault(); e.stopPropagation(); } }, true);
    el("co-mostrar").addEventListener("change", (e) => campos.forEach((c) => { c.type = e.target.checked ? "text" : "password"; }));
    campos.forEach((c) => c.addEventListener("input", () => mostrarError("")));
    el("co-salir").addEventListener("click", () => logout());

    el("co-form").addEventListener("submit", async (e) => {
      e.preventDefault();
      if (enviando) return;
      const [actual, nueva, confirmar] = campos.map((c) => c.value);
      let falla = "";
      if (!actual.trim()) falla = "Escriba la contraseña temporal que le entregó la administración.";
      else if (!nueva.trim() || nueva.length < 8 || nueva.length > 128) falla = "La nueva contraseña debe tener entre 8 y 128 caracteres.";
      else if (nueva === actual) falla = "La nueva contraseña debe ser distinta de la temporal.";
      else if (confirmar !== nueva) falla = "La confirmación no coincide con la nueva contraseña.";
      if (falla) { mostrarError(falla); return; }

      enviando = true;
      boton.disabled = true;
      boton.textContent = "Guardando...";
      try {
        const r = await window.Api.put("api/Usuarios/CambiarPassword/" + u.idUsuario, {
          passwordActual: actual, nuevaPassword: nueva, confirmarPassword: confirmar
        });
        // Se cierra la sesión y se pide entrar con la contraseña nueva: así se comprueba que el usuario
        // la recuerda mientras la administración todavía está presente para restablecerla si hiciera falta.
        if (r && r.token) guardarToken(r.token);
        await logout("password");
      } catch (err) {
        if (err && err.status === 401) return;   // Api ya cerró la sesión y llevó al inicio
        mostrarError(window.Api.mensajeError(err, "No fue posible cambiar la contraseña. Intente de nuevo."));
        enviando = false;
        boton.disabled = false;
        boton.textContent = "Guardar contraseña";
      }
    });

    setTimeout(() => campos[0].focus(), 30);
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

    // Con permisos en caché la página se habilita de inmediato y la consulta de permisos
    // se actualiza en segundo plano; sin caché, la página espera la respuesta.
    const habiaCache = permisosCargados;
    if (habiaCache) aplicarPermisosPagina();
    const refresco = cargarPermisos(true).then(aplicarPermisosPagina);
    if (!habiaCache) await refresco;
  }

  /** Quita del menú las páginas sin permiso y redirige si la página actual dejó de estar permitida. */
  function aplicarPermisosPagina() {
    if (!permisosCargados) return;
    Object.keys(PERMISO_PAGINA).forEach((pg) => {
      if (!tiene(PERMISO_PAGINA[pg])) {
        document.querySelectorAll('.sidebar a[href$="' + pg + '"]').forEach((a) => a.remove());
      }
    });
    const req = PERMISO_PAGINA[pagina];
    if (req && !tiene(req)) location.replace(paginaInicio());
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
    if (enPublica) resolverListo();
    else irALogin(habiaToken ? "expirada" : "");
  } else if (cambioPendiente()) {
    // Contraseña temporal: no se cargan permisos ni datos de la página ("listo" no se resuelve).
    programarExpiracion();
    document.addEventListener("DOMContentLoaded", mostrarCambioObligatorio);
  } else {
    programarExpiracion();
    ["pointerdown", "keydown", "wheel", "touchstart"].forEach((evento) =>
      document.addEventListener(evento, registrarActividad, { passive: true, capture: true }));

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
    exigirCambioPassword: mostrarCambioObligatorio,
    get permisosCargados() { return permisosCargados; },
    mostrarAviso
  };
})();