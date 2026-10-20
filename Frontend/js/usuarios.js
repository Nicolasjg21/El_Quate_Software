/**
 * @file usuarios.js
 * @description Módulo Usuarios (requiere "usuarios.gestionar").
 *   - Alta, edición y eliminación de usuarios según UsuarioCrearDTO y UsuarioActualizarDTO.
 *   - La contraseña solo se define al crear; después se cambia con CambiarPassword.
 *   - El usuario de la sesión no puede eliminarse ni cambiar su propio rol o estado: el Backend
 *     invalidaría su sesión y el sistema podría quedar sin administrador.
 *   - Roles y permisos (requiere "seguridad.gestionar"): creación de roles y asignación de permisos por rol.
 * @requires admin-data.js, admin-ui.js, data.js, auth.js
 */

/** Estado de la pantalla. */
let usuarios = [];
let textoBusqueda = "";
let usuarioEnEdicion = null;
let usuarioAEliminar = null;

/** Formato de correo exigido en la interfaz. */
const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Límites de UsuarioCrearDTO y UsuarioActualizarDTO. */
const LIMITES = { nombres: 200, apellidos: 200, documento: 50, telefono: 20, email: 254, passwordMin: 8, passwordMax: 128 };

/** Campo del DTO → id del control, para mostrar los errores 400 del Backend junto a cada campo. */
const CAMPOS_FORM = {
  nombres: "u-nombres", apellidos: "u-apellidos", documento: "u-documento", idtipodocumento: "u-tipo-documento",
  telefono: "u-telefono", estado: "u-estado", idrol: "u-rol", email: "u-email", password: "u-password"
};

const $ = (id) => document.getElementById(id);

/** Id del usuario de la sesión. */
const idPropio = () => { const u = Auth.usuario(); return u ? u.idUsuario : null; };
const MSG_CORREO_REPETIDO = "Ya existe un usuario registrado con ese correo electrónico.";

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  conectarCierreModales();
  limpiarErrorAlEscribir($("form-usuario"));

  $("buscar-usuarios").addEventListener("input", (e) => {
    textoBusqueda = e.target.value;
    renderTabla();
  });
  $("btn-nuevo-usuario").addEventListener("click", abrirNuevo);
  $("btn-guardar-usuario").addEventListener("click", guardarUsuario);
  $("btn-confirmar-eliminar").addEventListener("click", confirmarEliminacion);

  document.querySelectorAll('[data-cerrar="overlay-usuario"]').forEach(b => b.addEventListener("click", vaciarPassword));
  document.addEventListener("keydown", (e) => { if (e.key === "Escape") vaciarPassword(); });

  $("tabla-usuarios").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (!btn) return;
    const id = Number(btn.dataset.id);
    if (btn.dataset.accion === "editar") abrirEdicion(id);
    if (btn.dataset.accion === "eliminar") abrirEliminar(id);
  });

  $("btn-roles-permisos").addEventListener("click", abrirRoles);
  $("btn-crear-rol").addEventListener("click", crearRol);
  $("input-nuevo-rol").addEventListener("keydown", (e) => { if (e.key === "Enter") crearRol(); });
  $("btn-guardar-permisos").addEventListener("click", guardarPermisosRol);
  $("lista-roles").addEventListener("click", (e) => {
    const b = e.target.closest("[data-rol]");
    if (b) seleccionarRol(Number(b.dataset.rol));
  });
  $("lista-permisos").addEventListener("change", (e) => {
    const c = e.target.closest("input[data-permiso]");
    if (!c) return;
    const id = Number(c.dataset.permiso);
    if (c.checked) seg.marcados.add(id); else seg.marcados.delete(id);
    actualizarBotonGuardar();
  });

  Auth.listo
    .then(() => { $("btn-roles-permisos").hidden = !Auth.tiene("seguridad.gestionar"); })
    .then(() => cargarCatalogos())
    .catch(() => mostrarToast("No fue posible cargar los catálogos de roles y tipos de documento.", "error"))
    .finally(() => { poblarSelects(); cargarUsuarios(); });
});

/* ---------- Catálogos ---------- */
const nombreTipoDocumento = (id) => (TIPOS_DOCUMENTO.find(t => t.idTipoDocumento === id) || {}).nombre || "—";
const nombreRol = (id) => (ROLES.find(r => r.idRol === id) || {}).nombre || "—";
const textoEstado = (estado) => estado ? "Activo" : "Inactivo";

function poblarSelects() {
  $("u-tipo-documento").innerHTML = `<option value="">Seleccione una opción</option>` +
    TIPOS_DOCUMENTO.map(t => `<option value="${t.idTipoDocumento}">${esc(t.nombre)}</option>`).join("");
  $("u-rol").innerHTML = `<option value="">Seleccione una opción</option>` +
    ROLES.map(r => `<option value="${r.idRol}">${esc(r.nombre)}</option>`).join("");
}

/* ---------- Consulta ---------- */
function cargarUsuarios() {
  $("tabla-usuarios").innerHTML = filaMensaje("Cargando usuarios...");
  apiUsuarios.listar()
    .then(datos => { usuarios = datos; renderTabla(); })
    .catch(err => {
      $("tabla-usuarios").innerHTML = filaMensaje(Api.mensajeError(err, "No fue posible cargar los usuarios. Intente de nuevo."));
    });
}

function filaMensaje(texto, conBoton) {
  const boton = conBoton ? `<div><button class="btn btn--primario" data-nuevo-vacio>+ Nuevo usuario</button></div>` : "";
  return `<tr class="fila-mensaje"><td colspan="10" class="estado-tabla"><p>${esc(texto)}</p>${boton}</td></tr>`;
}

/** Búsqueda en todos los campos visibles; "activo" coincide solo con usuarios activos. */
function coincide(u, q) {
  const qDigitos = q.replace(/\D/g, "");
  const estado = normalizar(textoEstado(u.estado));
  const campos = [
    String(u.idUsuario), u.nombres, u.apellidos, u.documento,
    nombreTipoDocumento(u.idTipoDocumento), u.telefono, nombreRol(u.idRol), u.email
  ].map(normalizar);

  if (campos.some(c => c.includes(q))) return true;
  if (qDigitos && normalizar(u.telefono).replace(/\D/g, "").includes(qDigitos)) return true;
  return q === "activo" ? estado === "activo" : estado.includes(q);
}

function renderTabla() {
  const cuerpo = $("tabla-usuarios");
  const q = normalizar(textoBusqueda);
  const visibles = usuarios.filter(u => !q || coincide(u, q));

  if (usuarios.length === 0) {
    cuerpo.innerHTML = filaMensaje("No hay usuarios registrados.", true);
    const btn = cuerpo.querySelector("[data-nuevo-vacio]");
    if (btn) btn.addEventListener("click", abrirNuevo);
  } else if (visibles.length === 0) {
    cuerpo.innerHTML = filaMensaje("No se encontraron usuarios.");
  } else {
    cuerpo.innerHTML = visibles.map(filaUsuario).join("");
  }
  $("tabla-pie").textContent = `Mostrando ${visibles.length} de ${usuarios.length} usuarios`;
}

/** Color de la etiqueta del rol. Los roles sin color asignado usan el estilo neutro. */
function claseRol(idRol) {
  return { 1: "pill--rol-admin", 2: "pill--rol-caja", 3: "pill--rol-mesero" }[idRol] || "pill--neutro";
}

function filaUsuario(u) {
  const nombreCompleto = `${u.nombres} ${u.apellidos}`;
  return `
    <tr data-id="${u.idUsuario}">
      <td class="col-centro col-id">${u.idUsuario}</td>
      <td class="col-nombre">${esc(u.nombres)}</td>
      <td class="col-nombre">${esc(u.apellidos)}</td>
      <td>${esc(u.documento)}</td>
      <td>${esc(nombreTipoDocumento(u.idTipoDocumento))}</td>
      <td class="col-nombre">${esc(u.telefono)}</td>
      <td class="col-centro"><span class="pill ${u.estado ? "pill--verde" : "pill--rojo"}">${textoEstado(u.estado)}</span></td>
      <td class="col-centro"><span class="pill ${claseRol(u.idRol)}">${esc(nombreRol(u.idRol))}</span></td>
      <td>${esc(u.email)}</td>
      <td class="col-centro">
        <div class="acciones-fila">
          <button class="btn-icono" data-accion="editar" data-id="${u.idUsuario}" aria-label="Editar usuario ${esc(nombreCompleto)}" title="Editar usuario">${ICONO_EDITAR}</button>
          ${u.idUsuario === idPropio() ? "" : `<button class="btn-icono btn-icono--peligro" data-accion="eliminar" data-id="${u.idUsuario}" aria-label="Eliminar usuario ${esc(nombreCompleto)}" title="Eliminar usuario">${ICONO_ELIMINAR}</button>`}
        </div>
      </td>
    </tr>`;
}

/* ---------- Formulario ---------- */

/** Borra la contraseña escrita para que no permanezca en el formulario oculto. */
function vaciarPassword() { $("u-password").value = ""; }

function vaciarFormulario() {
  ["u-nombres", "u-apellidos", "u-documento", "u-telefono", "u-email", "u-password", "u-tipo-documento", "u-rol", "u-estado"]
    .forEach(id => { $(id).value = ""; });
  $("u-rol").disabled = false;
  $("u-estado").disabled = false;
  limpiarErrores($("form-usuario"));
}

function abrirNuevo() {
  usuarioEnEdicion = null;
  vaciarFormulario();
  $("titulo-usuario").textContent = "Nuevo usuario";
  $("descripcion-usuario").textContent = "Registra la información del usuario.";
  $("btn-guardar-usuario").textContent = "Guardar usuario";
  $("fila-password").hidden = false;
  abrirModal("overlay-usuario");
}

/** Abre la edición. En el propio usuario, el rol y el estado quedan bloqueados. */
function abrirEdicion(id) {
  const u = usuarios.find(x => x.idUsuario === id);
  if (!u) return;
  usuarioEnEdicion = id;
  vaciarFormulario();
  $("u-nombres").value = u.nombres;
  $("u-apellidos").value = u.apellidos;
  $("u-documento").value = u.documento;
  $("u-telefono").value = u.telefono;
  $("u-tipo-documento").value = String(u.idTipoDocumento);
  $("u-rol").value = String(u.idRol);
  $("u-estado").value = u.estado ? "activo" : "inactivo";
  $("u-email").value = u.email;
  $("titulo-usuario").textContent = "Editar usuario";
  const esPropio = u.idUsuario === idPropio();
  $("u-rol").disabled = esPropio;
  $("u-estado").disabled = esPropio;
  $("descripcion-usuario").textContent = esPropio
    ? `ID del usuario: ${u.idUsuario} · Es su propio usuario: el rol y el estado no se pueden cambiar desde aquí.`
    : `ID del usuario: ${u.idUsuario}`;
  $("btn-guardar-usuario").textContent = "Guardar cambios";
  $("fila-password").hidden = true;
  abrirModal("overlay-usuario");
}

/* ---------- Validación ---------- */

/**
 * Valida el formulario con las reglas del DTO y detecta correos repetidos antes de enviar.
 * Una contraseña compuesta solo por espacios se considera vacía, igual que [Required].
 * @returns {boolean}
 */
function validarFormulario() {
  limpiarErrores($("form-usuario"));
  let valido = true;
  const falla = (id, msg) => { marcarError($(id), msg); valido = false; };

  const texto = (id, etiqueta, max) => {
    const v = $(id).value.trim();
    if (!v) falla(id, `${etiqueta} es obligatorio.`);
    else if (v.length > max) falla(id, `${etiqueta} no puede superar ${max} caracteres.`);
  };
  texto("u-nombres", "El nombre", LIMITES.nombres);
  texto("u-apellidos", "El apellido", LIMITES.apellidos);
  texto("u-documento", "El documento", LIMITES.documento);
  texto("u-telefono", "El teléfono", LIMITES.telefono);
  if (!$("u-tipo-documento").value) falla("u-tipo-documento", "Seleccione un tipo de documento.");
  if (!$("u-rol").value) falla("u-rol", "Seleccione un rol.");
  if (!$("u-estado").value) falla("u-estado", "Seleccione un estado.");

  const email = $("u-email").value.trim();
  if (!email) falla("u-email", "El correo electrónico es obligatorio.");
  else if (email.length > LIMITES.email) falla("u-email", `El correo electrónico no puede superar ${LIMITES.email} caracteres.`);
  else if (!REGEX_EMAIL.test(email)) falla("u-email", "El correo electrónico no tiene un formato válido.");
  else if (usuarios.some(x => x.idUsuario !== usuarioEnEdicion && String(x.email).toLowerCase() === email.toLowerCase())) {
    falla("u-email", MSG_CORREO_REPETIDO);
  }

  if (usuarioEnEdicion === null) {
    const pw = $("u-password").value;
    if (!pw || !pw.trim()) falla("u-password", "La contraseña es obligatoria.");
    else if (pw.length < LIMITES.passwordMin || pw.length > LIMITES.passwordMax) {
      falla("u-password", `La contraseña debe tener entre ${LIMITES.passwordMin} y ${LIMITES.passwordMax} caracteres.`);
    }
  }

  if (!valido) {
    const primero = $("form-usuario").querySelector(".campo-error");
    if (primero) primero.focus();
  }
  return valido;
}

/** Datos del formulario con la forma del DTO. La contraseña solo se incluye al crear. */
function leerFormulario() {
  const datos = {
    nombres: $("u-nombres").value.trim(),
    apellidos: $("u-apellidos").value.trim(),
    documento: $("u-documento").value.trim(),
    idTipoDocumento: Number($("u-tipo-documento").value),
    telefono: $("u-telefono").value.trim(),
    estado: $("u-estado").value === "activo",
    idRol: Number($("u-rol").value),
    email: $("u-email").value.trim()
  };
  if (usuarioEnEdicion === null) datos.password = $("u-password").value;
  return datos;
}

/** Muestra junto a cada campo los errores 400 devueltos por el Backend. */
function mostrarErroresServidor(err) {
  let alguno = false;
  Object.keys((err && err.errores) || {}).forEach(campo => {
    const id = CAMPOS_FORM[campo.toLowerCase()];
    if (id && $(id)) { marcarError($(id), err.errores[campo][0]); alguno = true; }
  });
  return alguno;
}

/* ---------- Alta y edición ---------- */

/** Pide confirmación con el resumen de datos o de cambios y envía el POST o el PUT. */
async function guardarUsuario() {
  if (!validarFormulario()) return;
  const btn = $("btn-guardar-usuario");
  if (btn.disabled) return;
  const datos = leerFormulario();
  const editando = usuarioEnEdicion !== null;

  const textoSel = (id) => { const s = $(id); return s.options[s.selectedIndex] ? s.options[s.selectedIndex].text : ""; };
  if (editando) {
    const u = usuarios.find(x => x.idUsuario === usuarioEnEdicion);
    const antes = { n: u.nombres, a: u.apellidos, td: nombreTipoDocumento(u.idTipoDocumento), d: u.documento, t: u.telefono, e: u.email, r: nombreRol(u.idRol), s: u.estado ? "Activo" : "Inactivo" };
    const despues = { n: datos.nombres, a: datos.apellidos, td: nombreTipoDocumento(datos.idTipoDocumento), d: datos.documento, t: datos.telefono, e: datos.email, r: nombreRol(datos.idRol), s: datos.estado ? "Activo" : "Inactivo" };
    const filas = Confirmar.cambios(antes, despues, { n: "Nombres", a: "Apellidos", td: "Tipo de documento", d: "Documento", t: "Teléfono", e: "Correo", r: "Rol", s: "Estado" });
    if (!filas.length) { mostrarToast("No hay cambios para guardar."); return; }
    const ok = await Confirmar.pedir({ titulo: "Confirmar edición", mensaje: "Se modificará el usuario <strong>" + esc(u.nombres) + " " + esc(u.apellidos) + "</strong>. ¿Desea guardar los cambios?", filas, textoConfirmar: "Sí, guardar cambios" });
    if (!ok) return;
  } else {
    const ok = await Confirmar.pedir({
      titulo: "Verifique los datos del nuevo usuario",
      mensaje: "Revise que la información sea correcta antes de crear el usuario.",
      filas: [["Nombres", esc(datos.nombres)], ["Apellidos", esc(datos.apellidos)], ["Tipo de documento", esc(textoSel("u-tipo-documento"))], ["Documento", esc(datos.documento)],
        ["Teléfono", esc(datos.telefono)], ["Correo", esc(datos.email)], ["Rol", esc(textoSel("u-rol"))], ["Estado", datos.estado ? "Activo" : "Inactivo"], ["Contraseña", "&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;"]],
      textoConfirmar: "Sí, crear usuario"
    });
    if (!ok) return;
  }

  bloquearBoton(btn, "Guardando...");
  const peticion = editando
    ? apiUsuarios.actualizar(usuarioEnEdicion, { ...datos, idUsuario: usuarioEnEdicion }, usuarios.find(u => u.idUsuario === usuarioEnEdicion))
    : apiUsuarios.crear(datos);

  peticion
    .then(guardado => {
      if (editando) {
        const idx = usuarios.findIndex(u => u.idUsuario === guardado.idUsuario);
        if (idx !== -1) usuarios[idx] = guardado;
      } else {
        usuarios.push(guardado);
      }
      vaciarPassword();
      cerrarModal("overlay-usuario");
      renderTabla();
      mostrarToast(editando ? "Usuario actualizado correctamente." : "Usuario creado correctamente.");
    })
    .catch(err => {
      if (err && err.status === 400) mostrarErroresServidor(err);
      if (err && err.status === 409 && /correo/i.test(err.mensaje || "") && /^Ya existe/i.test(err.mensaje || "")) {
        marcarError($("u-email"), err.mensaje);
      }
      mostrarToast(Api.mensajeError(err, "No fue posible guardar el usuario. Intente de nuevo."), "error");
    })
    .finally(() => liberarBoton(btn));
}

/* ---------- Eliminación ----------
   Un usuario con registros asociados no puede eliminarse (409); se recomienda desactivarlo. */
function abrirEliminar(id) {
  const u = usuarios.find(x => x.idUsuario === id);
  if (!u) return;
  usuarioAEliminar = id;
  $("texto-eliminar").innerHTML = `¿Está seguro de que desea eliminar a <strong>${esc(u.nombres)} ${esc(u.apellidos)}</strong>?`;
  abrirModal("overlay-eliminar");
}

function confirmarEliminacion() {
  if (usuarioAEliminar === null) return;
  const btn = $("btn-confirmar-eliminar");
  if (btn.disabled) return;
  const id = usuarioAEliminar;

  bloquearBoton(btn, "Eliminando...");
  apiUsuarios.eliminar(id, usuarios.find(u => u.idUsuario === id))
    .then(() => {
      usuarios = usuarios.filter(u => u.idUsuario !== id);
      cerrarModal("overlay-eliminar");
      renderTabla();
      mostrarToast("Usuario eliminado correctamente.");
    })
    .catch(err => {
      cerrarModal("overlay-eliminar");
      mostrarToast(Api.mensajeError(err, "No fue posible eliminar el usuario. Intente de nuevo."), "error");
    })
    .finally(() => { liberarBoton(btn); usuarioAEliminar = null; });
}

/* ==================== Roles y permisos ====================
   Los permisos se asignan por rol (tabla RolesPermisos). Para otorgar un permiso adicional a una
   persona se crea un rol con los permisos necesarios y se le asigna en su usuario.
   El Backend conserva en memoria los permisos de cada rol hasta 60 segundos y cada página
   los consulta de nuevo al cargarse. */

/** Permisos que utiliza el sistema, con su descripción para el administrador. */
const PERMISOS_SISTEMA = {
  "usuarios.gestionar":   { t: "Administrar usuarios", d: "Entrar a Usuarios y crear, editar o eliminar usuarios." },
  "seguridad.gestionar":  { t: "Administrar roles, permisos y mesas", d: "Crear roles y asignar permisos; crear, eliminar o retirar mesas." },
  "auditorias.consultar": { t: "Ver Panel, Analíticas y Auditoría", d: "Acceso al Panel Principal, a Ventas y Analíticas y al registro de Auditoría." },
  "inventario.gestionar": { t: "Administrar inventario y compras", d: "Crear y editar productos, categorías y proveedores; ajustar stock; registrar compras." },
  "cobros.gestionar":     { t: "Cobrar cuentas", d: "Registrar con qué pagó el cliente y emitir el comprobante de pago." },
  "auditorias.modificar": { t: "Corregir registros de auditoría", d: "Solo desde la API: editar o borrar auditorías. No se recomienda otorgarlo." },
  "kardex.modificar":     { t: "Corregir movimientos de stock", d: "Solo desde la API: editar o borrar movimientos del Kardex." }
};

/** Permisos que no pueden retirarse del propio rol: sin ellos se perdería el acceso a esta pantalla. */
const PERMISOS_PROTEGIDOS = ["seguridad.gestionar", "usuarios.gestionar"];

/** Estado de la ventana de roles: datos cargados, rol seleccionado y permisos originales y marcados. */
const seg = { roles: [], permisos: [], relaciones: [], rolSel: null, originales: new Set(), marcados: new Set(), ocupado: false };

const idRolPropio = () => { const u = Auth.usuario(); return u ? u.idRol : null; };

async function abrirRoles() {
  $("error-roles").textContent = "";
  $("lista-roles").innerHTML = `<p class="modulo-subtitulo">Cargando roles...</p>`;
  $("lista-permisos").innerHTML = "";
  abrirModal("overlay-roles");
  try {
    Object.assign(seg, await apiSeguridad.cargar());
    seleccionarRol(seg.rolSel && seg.roles.some(r => r.idRol === seg.rolSel) ? seg.rolSel : (seg.roles[0] || {}).idRol);
  } catch (err) {
    $("lista-roles").innerHTML = "";
    $("error-roles").textContent = Api.mensajeError(err, "No fue posible cargar los roles y permisos.");
  }
}

function renderRoles() {
  $("lista-roles").innerHTML = seg.roles.map(r => {
    const n = usuarios.filter(u => u.idRol === r.idRol).length;
    return `<button type="button" class="rol-item${r.idRol === seg.rolSel ? " rol-item--activo" : ""}" data-rol="${r.idRol}" role="option" aria-selected="${r.idRol === seg.rolSel}">
      <span>${esc(r.nombreRol)}${r.idRol === idRolPropio() ? " (su rol)" : ""}</span><small>${n} ${n === 1 ? "usuario" : "usuarios"}</small></button>`;
  }).join("") || `<p class="modulo-subtitulo">No hay roles.</p>`;
}

/** Selecciona un rol y carga sus permisos actuales. */
function seleccionarRol(idRol) {
  if (!idRol) { renderRoles(); return; }
  seg.rolSel = idRol;
  seg.originales = new Set(seg.relaciones.filter(x => x.idRol === idRol).map(x => x.idPermiso));
  seg.marcados = new Set(seg.originales);
  $("error-roles").textContent = "";
  renderRoles();
  renderPermisos();
}

/** Casilla de un permiso. Los permisos que el sistema no utiliza se identifican como tales. */
function itemPermiso(p, bloqueado) {
  const info = PERMISOS_SISTEMA[p.nombrePermiso];
  const marcado = seg.marcados.has(p.idPermiso);
  return `<label class="permiso-item${bloqueado ? " permiso-item--bloqueado" : ""}${info ? "" : " permiso-item--sin-uso"}">
    <input type="checkbox" data-permiso="${p.idPermiso}" ${marcado ? "checked" : ""} ${bloqueado ? "disabled" : ""}>
    <div><strong>${esc(info ? info.t : p.nombrePermiso)}</strong>
      <span>${esc(info ? info.d : "El sistema no revisa este permiso actualmente: marcarlo o desmarcarlo no cambia nada.")}</span>
      ${bloqueado ? `<span>No puede quitárselo a su propio rol: perdería el acceso a esta pantalla.</span>` : ""}
      <code>${esc(p.nombrePermiso)}</code></div>
  </label>`;
}

function renderPermisos() {
  const rol = seg.roles.find(r => r.idRol === seg.rolSel);
  if (!rol) return;
  const propio = rol.idRol === idRolPropio();
  const n = usuarios.filter(u => u.idRol === rol.idRol).length;
  $("titulo-permisos-rol").textContent = `Permisos del rol "${rol.nombreRol}"`;
  $("usuarios-rol").textContent = n
    ? `Los cambios afectan a ${n} ${n === 1 ? "usuario" : "usuarios"} con este rol.`
    : "Ningún usuario tiene este rol todavía.";
  const usados = seg.permisos.filter(p => PERMISOS_SISTEMA[p.nombrePermiso]);
  const otros = seg.permisos.filter(p => !PERMISOS_SISTEMA[p.nombrePermiso]);
  const bloq = (p) => propio && PERMISOS_PROTEGIDOS.includes(p.nombrePermiso) && seg.originales.has(p.idPermiso);
  $("lista-permisos").innerHTML =
    `<div class="permiso-grupo"><p class="permiso-grupo-titulo">Permisos que usa el sistema</p>${usados.map(p => itemPermiso(p, bloq(p))).join("") || `<p class="modulo-subtitulo">No hay permisos registrados.</p>`}</div>` +
    (otros.length ? `<div class="permiso-grupo"><p class="permiso-grupo-titulo">Otros permisos registrados en la base (sin efecto)</p>${otros.map(p => itemPermiso(p, false)).join("")}</div>` : "");
  actualizarBotonGuardar();
}

/** Diferencia entre los permisos originales y los marcados. */
function cambiosPendientes() {
  const agregar = [...seg.marcados].filter(id => !seg.originales.has(id));
  const quitar = [...seg.originales].filter(id => !seg.marcados.has(id));
  return { agregar, quitar };
}

function actualizarBotonGuardar() {
  const { agregar, quitar } = cambiosPendientes();
  const n = agregar.length + quitar.length;
  const btn = $("btn-guardar-permisos");
  btn.disabled = n === 0 || seg.ocupado;
  btn.textContent = n ? `Guardar ${n} ${n === 1 ? "cambio" : "cambios"}` : "Guardar permisos del rol";
}

/** Confirma y aplica las altas y bajas de permisos del rol seleccionado, informando las que fallen. */
async function guardarPermisosRol() {
  if (seg.ocupado) return;
  const rol = seg.roles.find(r => r.idRol === seg.rolSel);
  const { agregar, quitar } = cambiosPendientes();
  if (!rol || !(agregar.length + quitar.length)) return;
  const nombre = (id) => { const p = seg.permisos.find(x => x.idPermiso === id); const i = p && PERMISOS_SISTEMA[p.nombrePermiso]; return i ? i.t : (p ? p.nombrePermiso : "#" + id); };
  const filas = [
    ...agregar.map(id => ["Otorgar", esc(nombre(id))]),
    ...quitar.map(id => ["Quitar", esc(nombre(id))])
  ];
  const ok = await Confirmar.pedir({
    titulo: "Confirmar permisos",
    mensaje: `Se actualizarán los permisos del rol <strong>${esc(rol.nombreRol)}</strong>. Los usuarios con este rol verán el cambio al recargar la página (el servidor puede tardar hasta 1 minuto en aplicarlo).`,
    filas, textoConfirmar: "Sí, guardar"
  });
  if (!ok) return;

  seg.ocupado = true;
  actualizarBotonGuardar();
  const errores = [];
  for (const id of agregar) {
    const p = seg.permisos.find(x => x.idPermiso === id);
    try { await apiSeguridad.asignar(rol, p); } catch (e) { errores.push(`${nombre(id)}: ${Api.mensajeError(e)}`); }
  }
  for (const id of quitar) {
    const p = seg.permisos.find(x => x.idPermiso === id);
    try { await apiSeguridad.quitar(rol, p); } catch (e) { errores.push(`${nombre(id)}: ${Api.mensajeError(e)}`); }
  }
  seg.ocupado = false;
  try { Object.assign(seg, await apiSeguridad.cargar()); } catch (e) { }
  seleccionarRol(rol.idRol);
  if (errores.length) {
    $("error-roles").textContent = "Algunos cambios no se guardaron: " + errores.join(" · ");
    mostrarToast("Algunos permisos no se pudieron guardar.", "error");
  } else {
    mostrarToast(`Permisos del rol "${rol.nombreRol}" actualizados.`);
  }
}

/** Crea un rol y lo agrega también al formulario de usuarios. */
async function crearRol() {
  if (seg.ocupado) return;
  const input = $("input-nuevo-rol");
  const error = $("error-nuevo-rol");
  const nombre = input.value.trim();
  const falla = (t) => { error.textContent = t; error.classList.add("campo-mensaje-error--visible"); input.classList.add("campo-error"); };
  error.classList.remove("campo-mensaje-error--visible");
  input.classList.remove("campo-error");
  if (!nombre) return falla("Escriba el nombre del rol.");
  if (nombre.length > 50) return falla("El nombre no puede superar 50 caracteres.");
  if (seg.roles.some(r => normalizar(r.nombreRol) === normalizar(nombre))) return falla("Ya existe un rol con ese nombre.");

  seg.ocupado = true;
  try {
    const nuevo = await apiSeguridad.crearRol(nombre);
    input.value = "";
    Object.assign(seg, await apiSeguridad.cargar());
    await cargarCatalogos().then(poblarSelects).catch(() => {});
    seleccionarRol(nuevo && nuevo.idRol ? nuevo.idRol : seg.rolSel);
    mostrarToast(`Rol "${nombre}" creado. Márquele los permisos y guárdelos.`);
  } catch (err) {
    falla(Api.mensajeError(err, "No fue posible crear el rol."));
  } finally {
    seg.ocupado = false;
    actualizarBotonGuardar();
  }
}
