/* ==========================================================================
   USUARIOS.JS — CRUD del módulo Usuarios
   Campos: idUsuario, nombres, apellidos, documento, idTipoDocumento,
   telefono, estado (bool), idRol, email.
   ========================================================================== */

let usuarios = [];          // caché de la última consulta (GET)
let textoBusqueda = "";
let usuarioEnEdicion = null; // idUsuario en edición (null = creando)
let usuarioAEliminar = null; // idUsuario pendiente de confirmar

const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/* Límites de UsuarioCrearDTO / UsuarioActualizarDTO del Backend */
const LIMITES = { nombres: 200, apellidos: 200, documento: 50, telefono: 20, email: 254, passwordMin: 8, passwordMax: 128 };

/* Campo del DTO -> id del control del formulario (para pintar errores 400 del Backend) */
const CAMPOS_FORM = {
  nombres: "u-nombres", apellidos: "u-apellidos", documento: "u-documento", idtipodocumento: "u-tipo-documento",
  telefono: "u-telefono", estado: "u-estado", idrol: "u-rol", email: "u-email", password: "u-password"
};

const $ = (id) => document.getElementById(id);

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

  /* Delegación: el usuario se identifica por data-id (nunca por índice de fila) */
  $("tabla-usuarios").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (!btn) return;
    const id = Number(btn.dataset.id);
    if (btn.dataset.accion === "editar") abrirEdicion(id);
    if (btn.dataset.accion === "eliminar") abrirEliminar(id);
  });

  /* Espera a que Auth cargue los permisos y trae los catálogos (roles / tipos de documento) */
  Auth.listo
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

/* ---------- GET ---------- */
function cargarUsuarios() {
  $("tabla-usuarios").innerHTML = filaMensaje("Cargando usuarios...");
  apiUsuarios.listar()
    .then(datos => { usuarios = datos; renderTabla(); })
    .catch(err => {
      $("tabla-usuarios").innerHTML = filaMensaje(Api.mensajeError(err, "No fue posible cargar los usuarios. Intente de nuevo."));
    });
}

/* ---------- Render ---------- */
function filaMensaje(texto, conBoton) {
  const boton = conBoton ? `<div><button class="btn btn--primario" data-nuevo-vacio>+ Nuevo usuario</button></div>` : "";
  return `<tr class="fila-mensaje"><td colspan="10" class="estado-tabla"><p>${esc(texto)}</p>${boton}</td></tr>`;
}

function coincide(u, q) {
  const qDigitos = q.replace(/\D/g, "");
  const estado = normalizar(textoEstado(u.estado));
  const campos = [
    String(u.idUsuario), u.nombres, u.apellidos, u.documento,
    nombreTipoDocumento(u.idTipoDocumento), u.telefono, nombreRol(u.idRol), u.email
  ].map(normalizar);

  if (campos.some(c => c.includes(q))) return true;
  if (qDigitos && normalizar(u.telefono).replace(/\D/g, "").includes(qDigitos)) return true;
  /* "activo" debe encontrar solo activos (no "Inactivo"); textos parciales sí coinciden */
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
          <button class="btn-icono btn-icono--peligro" data-accion="eliminar" data-id="${u.idUsuario}" aria-label="Eliminar usuario ${esc(nombreCompleto)}" title="Eliminar usuario">${ICONO_ELIMINAR}</button>
        </div>
      </td>
    </tr>`;
}

/* ---------- Formulario: abrir ---------- */
function vaciarFormulario() {
  ["u-nombres", "u-apellidos", "u-documento", "u-telefono", "u-email", "u-password", "u-tipo-documento", "u-rol", "u-estado"]
    .forEach(id => { $(id).value = ""; });
  limpiarErrores($("form-usuario"));
}

function abrirNuevo() {
  usuarioEnEdicion = null;
  vaciarFormulario();
  $("titulo-usuario").textContent = "Nuevo usuario";
  $("descripcion-usuario").textContent = "Registra la información del usuario.";
  $("btn-guardar-usuario").textContent = "Guardar usuario";
  $("fila-password").hidden = false;   // la contraseña solo se define al crear (POST); luego se cambia con CambiarPassword
  abrirModal("overlay-usuario");
}

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
  $("descripcion-usuario").textContent = `ID del usuario: ${u.idUsuario}`;
  $("btn-guardar-usuario").textContent = "Guardar cambios";
  $("fila-password").hidden = true;
  abrirModal("overlay-usuario");
}

/* ---------- Validación ---------- */
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

  if (usuarioEnEdicion === null) {
    const pw = $("u-password").value;
    if (!pw) falla("u-password", "La contraseña es obligatoria.");
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
  if (usuarioEnEdicion === null) datos.password = $("u-password").value;   // solo en POST (UsuarioCrearDTO)
  return datos;
}

/** Pinta en el formulario los errores 400 por campo que devuelve el Backend. */
function mostrarErroresServidor(err) {
  let alguno = false;
  Object.keys((err && err.errores) || {}).forEach(campo => {
    const id = CAMPOS_FORM[campo.toLowerCase()];
    if (id && $(id)) { marcarError($(id), err.errores[campo][0]); alguno = true; }
  });
  return alguno;
}

/* ---------- POST / PUT ---------- */
function guardarUsuario() {
  if (!validarFormulario()) return;
  const btn = $("btn-guardar-usuario");
  if (btn.disabled) return;
  const datos = leerFormulario();
  const editando = usuarioEnEdicion !== null;

  bloquearBoton(btn, "Guardando...");
  const peticion = editando
    ? apiUsuarios.actualizar(usuarioEnEdicion, { ...datos, idUsuario: usuarioEnEdicion })   // PUT
    : apiUsuarios.crear(datos);                                                              // POST

  peticion
    .then(guardado => {
      if (editando) {
        const idx = usuarios.findIndex(u => u.idUsuario === guardado.idUsuario);
        if (idx !== -1) usuarios[idx] = guardado;
      } else {
        usuarios.push(guardado);
      }
      cerrarModal("overlay-usuario");
      renderTabla();
      mostrarToast(editando ? "Usuario actualizado correctamente." : "Usuario creado correctamente.");
    })
    .catch(err => {
      if (err && err.status === 400) mostrarErroresServidor(err);
      /* 400 / 403 / 404 / 409 / 429 / 5xx: se muestra el mensaje uniforme del Backend */
      mostrarToast(Api.mensajeError(err, "No fue posible guardar el usuario. Intente de nuevo."), "error");
    })
    .finally(() => liberarBoton(btn));
}

/* ---------- DELETE ---------- */
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
  apiUsuarios.eliminar(id)
    .then(() => {
      usuarios = usuarios.filter(u => u.idUsuario !== id);
      cerrarModal("overlay-eliminar");
      renderTabla();
      mostrarToast("Usuario eliminado correctamente.");
    })
    .catch(err => {
      cerrarModal("overlay-eliminar");
      mostrarToast(
        err && err.status === 409 ? "No es posible eliminar este usuario porque tiene registros asociados."
        : Api.mensajeError(err, "No fue posible eliminar el usuario. Intente de nuevo."),
        "error"
      );
    })
    .finally(() => { liberarBoton(btn); usuarioAEliminar = null; });
}
