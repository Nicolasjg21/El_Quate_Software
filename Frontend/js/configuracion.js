/**
 * @file configuracion.js
 * @description Página Configuración, disponible para cualquier usuario con sesión activa.
 *   - Cambiar contraseña: valida según CambiarPasswordDTO (actual obligatoria; nueva entre 8 y 128
 *     caracteres; confirmación igual a la nueva). El Backend invalida los tokens anteriores y
 *     devuelve uno nuevo, que reemplaza al de la sesión actual.
 *   - Mi información: datos del usuario de la sesión, solo consulta.
 *   La opción visible se conserva en la URL (#password o #cuenta).
 * @requires api-cliente.js, auth.js, data.js, admin-ui.js
 *
 * Endpoints: api/Usuarios/CambiarPassword/{id}, api/Usuarios/GetUsuariosById/{id},
 *            api/TipoDocumento/GetTipoDocumento, api/Roles/GetRoles
 */

const $ = (id) => document.getElementById(id);

/** Límites de CambiarPasswordDTO. */
const PASSWORD_MIN = 8;
const PASSWORD_MAX = 128;

const OPCIONES = ["password", "cuenta"];
let guardando = false;
let cuentaCargada = false;

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  $("config-opcion").addEventListener("change", (e) => mostrarSeccion(e.target.value));
  window.addEventListener("hashchange", () => mostrarSeccion(opcionDeUrl()));

  const form = $("seccion-password");
  form.addEventListener("submit", (e) => { e.preventDefault(); guardarPassword(); });
  form.addEventListener("reset", () => setTimeout(() => { limpiarErrores(form); mensaje(""); evaluarRequisitos(); }, 0));
  form.querySelectorAll(".campo").forEach(limpiarErrorAlEscribir);
  ["cfg-actual", "cfg-nueva", "cfg-confirmar"].forEach((id) => $(id).addEventListener("input", evaluarRequisitos));
  $("cfg-mostrar").addEventListener("change", (e) => {
    ["cfg-actual", "cfg-nueva", "cfg-confirmar"].forEach((id) => { $(id).type = e.target.checked ? "text" : "password"; });
  });

  Auth.listo.then(() => mostrarSeccion(opcionDeUrl()));
});

/* ---------- Selección de la opción ---------- */

/** Opción indicada en la URL; por defecto, el cambio de contraseña. */
function opcionDeUrl() {
  const o = location.hash.replace("#", "");
  return OPCIONES.includes(o) ? o : "password";
}

/**
 * Muestra la sección elegida y la refleja en el selector y en la URL.
 * @param {"password"|"cuenta"} opcion
 */
function mostrarSeccion(opcion) {
  $("config-opcion").value = opcion;
  $("seccion-password").hidden = opcion !== "password";
  $("seccion-cuenta").hidden = opcion !== "cuenta";
  if (location.hash !== "#" + opcion) history.replaceState(null, "", "#" + opcion);
  if (opcion === "password") $("cfg-actual").focus();
  if (opcion === "cuenta" && !cuentaCargada) cargarCuenta();
}

/* ---------- Cambio de contraseña ---------- */

/**
 * Mensaje de resultado bajo el formulario.
 * @param {string} texto Vacío para ocultarlo.
 * @param {boolean} [error]
 */
function mensaje(texto, error) {
  const el = $("cfg-mensaje");
  el.textContent = texto;
  el.hidden = !texto;
  el.classList.toggle("config-mensaje--error", !!error);
  el.classList.toggle("config-mensaje--ok", !!texto && !error);
}

/** Marca en la lista de requisitos los que ya se cumplen. */
function evaluarRequisitos() {
  const actual = $("cfg-actual").value, nueva = $("cfg-nueva").value, conf = $("cfg-confirmar").value;
  const marcar = (id, ok) => $(id).classList.toggle("cumplido", ok);
  marcar("req-longitud", nueva.length >= PASSWORD_MIN && nueva.length <= PASSWORD_MAX);
  marcar("req-distinta", !!nueva && nueva !== actual);
  marcar("req-coincide", !!conf && conf === nueva);
}

/**
 * Valida el formulario. Una contraseña formada solo por espacios se considera vacía, como [Required].
 * @returns {boolean}
 */
function validar() {
  const form = $("seccion-password");
  limpiarErrores(form);
  const actual = $("cfg-actual").value, nueva = $("cfg-nueva").value, conf = $("cfg-confirmar").value;
  let ok = true;
  const falla = (id, texto) => { marcarError($(id), texto); ok = false; };

  if (!actual.trim()) falla("cfg-actual", "La contraseña actual es obligatoria.");
  if (!nueva.trim()) falla("cfg-nueva", "La nueva contraseña es obligatoria.");
  else if (nueva.length < PASSWORD_MIN || nueva.length > PASSWORD_MAX) falla("cfg-nueva", `Debe tener entre ${PASSWORD_MIN} y ${PASSWORD_MAX} caracteres.`);
  else if (nueva === actual) falla("cfg-nueva", "La nueva contraseña debe ser distinta de la actual.");
  if (!conf) falla("cfg-confirmar", "Confirme la nueva contraseña.");
  else if (conf !== nueva) falla("cfg-confirmar", "La confirmación no coincide con la nueva contraseña.");

  if (!ok) { const primero = form.querySelector(".campo-error"); if (primero) primero.focus(); }
  return ok;
}

/** Envía el cambio de contraseña y reemplaza el token de la sesión por el que devuelve el Backend. */
async function guardarPassword() {
  if (guardando) return;
  mensaje("");
  if (!validar()) return;
  const u = Auth.usuario();
  if (!u) return;

  guardando = true;
  const btn = $("cfg-guardar");
  bloquearBoton(btn, "Guardando...");
  try {
    const r = await Api.put("api/Usuarios/CambiarPassword/" + u.idUsuario, {
      passwordActual: $("cfg-actual").value,
      nuevaPassword: $("cfg-nueva").value,
      confirmarPassword: $("cfg-confirmar").value
    });
    if (r && r.token) Auth.reemplazarToken(r.token);
    ["cfg-actual", "cfg-nueva", "cfg-confirmar"].forEach((id) => { $(id).value = ""; });
    evaluarRequisitos();
    mensaje("Contraseña actualizada correctamente.");
  } catch (err) {
    /* 400: contraseña actual incorrecta o datos inválidos; 429: demasiados intentos. Un 401 lo gestiona api-cliente. */
    if (err && err.status === 400 && /actual/i.test(err.mensaje || "")) marcarError($("cfg-actual"), err.mensaje);
    if (err && err.status !== 401) mensaje(Api.mensajeError(err, "No fue posible cambiar la contraseña."), true);
  } finally {
    guardando = false;
    liberarBoton(btn);
  }
}

/* ---------- Mi información ---------- */

/** Carga los datos del usuario de la sesión con los nombres de su tipo de documento y su rol. */
async function cargarCuenta() {
  const u = Auth.usuario();
  if (!u) return;
  $("cfg-error-cuenta").hidden = true;
  try {
    const [r, tipos, roles] = await Promise.all([
      Api.get("api/Usuarios/GetUsuariosById/" + u.idUsuario),
      Api.lista("api/TipoDocumento/GetTipoDocumento").catch(() => []),
      Api.lista("api/Roles/GetRoles").catch(() => [])
    ]);
    const d = (r && r.data) || {};
    const tipo = tipos.find((t) => t.idTipoDocumento === d.idTipoDocumento);
    const rol = roles.find((x) => x.idRol === d.idRol);
    const poner = (id, v) => { $(id).textContent = v === undefined || v === null || v === "" ? "—" : v; };
    poner("dato-nombres", d.nombres);
    poner("dato-apellidos", d.apellidos);
    poner("dato-tipo", tipo ? tipo.nombreTipo : d.idTipoDocumento);
    poner("dato-documento", d.documento);
    poner("dato-telefono", d.telefono);
    poner("dato-email", d.email);
    poner("dato-rol", rol ? rol.nombreRol : d.idRol);
    poner("dato-estado", d.estado === undefined ? "" : d.estado ? "Activo" : "Inactivo");
    cuentaCargada = true;
  } catch (err) {
    const el = $("cfg-error-cuenta");
    el.textContent = Api.mensajeError(err, "No fue posible cargar la información de la cuenta.");
    el.hidden = false;
  }
}
