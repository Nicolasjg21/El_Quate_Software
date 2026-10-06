/* ==========================================================================
   PROVEEDORES.JS — CRUD del módulo Proveedores
   Campos: idProveedor, nombreProveedor, telefono (máx. 20),
   direccion (máx. 150). La colección "compras" es solo relación (no editable).
   ========================================================================== */

const MAX_NOMBRE = 200;
const MAX_TELEFONO = 20;
const MAX_DIRECCION = 150;

let proveedores = [];          // caché de la última consulta (GET)
let textoBusqueda = "";
let proveedorEnEdicion = null; // idProveedor en edición (null = creando)
let proveedorAEliminar = null;

const $ = (id) => document.getElementById(id);

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  conectarCierreModales();
  limpiarErrorAlEscribir($("form-proveedor"));

  $("buscar-proveedores").addEventListener("input", (e) => {
    textoBusqueda = e.target.value;
    renderTabla();
  });
  $("btn-nuevo-proveedor").addEventListener("click", abrirNuevo);
  $("btn-guardar-proveedor").addEventListener("click", guardarProveedor);
  $("btn-confirmar-eliminar").addEventListener("click", confirmarEliminacion);

  /* Delegación: el proveedor se identifica por data-id (nunca por índice de fila) */
  $("tabla-proveedores").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (!btn) return;
    const id = Number(btn.dataset.id);
    if (btn.dataset.accion === "editar") abrirEdicion(id);
    if (btn.dataset.accion === "eliminar") abrirEliminar(id);
  });

  /* La lectura es libre para cualquier usuario autenticado; escribir exige inventario.gestionar */
  Auth.listo.then(() => {
    if (!Auth.tiene("inventario.gestionar") && Auth.permisosCargados) {
      $("btn-nuevo-proveedor").hidden = true;
    }
    cargarProveedores();
  });
});

const puedeGestionar = () => Auth.tiene("inventario.gestionar") || !Auth.permisosCargados;

/* ---------- GET ---------- */
function cargarProveedores() {
  $("tabla-proveedores").innerHTML = filaMensaje("Cargando proveedores...");
  apiProveedores.listar()
    .then(datos => { proveedores = datos; renderTabla(); })
    .catch(err => {
      $("tabla-proveedores").innerHTML = filaMensaje(Api.mensajeError(err, "No fue posible cargar los proveedores. Intente de nuevo."));
    });
}

/* ---------- Render ---------- */
function filaMensaje(texto, conBoton) {
  const boton = conBoton ? `<div><button class="btn btn--primario" data-nuevo-vacio>+ Nuevo proveedor</button></div>` : "";
  return `<tr class="fila-mensaje"><td colspan="5" class="estado-tabla"><p>${esc(texto)}</p>${boton}</td></tr>`;
}

function coincide(p, q) {
  const qDigitos = q.replace(/\D/g, "");
  const campos = [String(p.idProveedor), p.nombreProveedor, p.telefono, p.direccion].map(normalizar);
  if (campos.some(c => c.includes(q))) return true;
  return !!qDigitos && normalizar(p.telefono).replace(/\D/g, "").includes(qDigitos);
}

function renderTabla() {
  const cuerpo = $("tabla-proveedores");
  const q = normalizar(textoBusqueda);
  const visibles = proveedores.filter(p => !q || coincide(p, q));

  if (proveedores.length === 0) {
    cuerpo.innerHTML = filaMensaje("No hay proveedores registrados.", puedeGestionar());
    const btn = cuerpo.querySelector("[data-nuevo-vacio]");
    if (btn) btn.addEventListener("click", abrirNuevo);
  } else if (visibles.length === 0) {
    cuerpo.innerHTML = filaMensaje("No se encontraron proveedores.");
  } else {
    cuerpo.innerHTML = visibles.map(filaProveedor).join("");
  }
  $("tabla-pie").textContent = `Mostrando ${visibles.length} de ${proveedores.length} proveedores`;
}

function filaProveedor(p) {
  return `
    <tr data-id="${p.idProveedor}">
      <td class="col-centro col-id">${p.idProveedor}</td>
      <td>${esc(p.nombreProveedor)}</td>
      <td class="col-nombre">${esc(p.telefono)}</td>
      <td>${esc(p.direccion)}</td>
      <td class="col-centro">
        ${puedeGestionar() ? `<div class="acciones-fila">
          <button class="btn-icono" data-accion="editar" data-id="${p.idProveedor}" aria-label="Editar proveedor ${esc(p.nombreProveedor)}" title="Editar proveedor">${ICONO_EDITAR}</button>
          <button class="btn-icono btn-icono--peligro" data-accion="eliminar" data-id="${p.idProveedor}" aria-label="Eliminar proveedor ${esc(p.nombreProveedor)}" title="Eliminar proveedor">${ICONO_ELIMINAR}</button>
        </div>` : ""}
      </td>
    </tr>`;
}

/* ---------- Formulario: abrir ---------- */
function vaciarFormulario() {
  ["p-nombre", "p-telefono", "p-direccion"].forEach(id => { $(id).value = ""; });
  limpiarErrores($("form-proveedor"));
}

function abrirNuevo() {
  proveedorEnEdicion = null;
  vaciarFormulario();
  $("titulo-proveedor").textContent = "Nuevo proveedor";
  $("descripcion-proveedor").textContent = "Registra la información del proveedor.";
  $("btn-guardar-proveedor").textContent = "Guardar proveedor";
  abrirModal("overlay-proveedor");
}

function abrirEdicion(id) {
  const p = proveedores.find(x => x.idProveedor === id);
  if (!p) return;
  proveedorEnEdicion = id;
  vaciarFormulario();
  $("p-nombre").value = p.nombreProveedor;
  $("p-telefono").value = p.telefono;
  $("p-direccion").value = p.direccion;
  $("titulo-proveedor").textContent = "Editar proveedor";
  $("descripcion-proveedor").textContent = `ID del proveedor: ${p.idProveedor}`;
  $("btn-guardar-proveedor").textContent = "Guardar cambios";
  abrirModal("overlay-proveedor");
}

/* ---------- Validación ---------- */
function validarFormulario() {
  limpiarErrores($("form-proveedor"));
  let valido = true;
  const falla = (id, msg) => { marcarError($(id), msg); valido = false; };

  const nom = $("p-nombre").value.trim();
  if (!nom) falla("p-nombre", "El nombre del proveedor es obligatorio.");
  else if (nom.length > MAX_NOMBRE) falla("p-nombre", "El nombre del proveedor no puede superar los 200 caracteres.");

  const tel = $("p-telefono").value.trim();
  if (!tel) falla("p-telefono", "El teléfono es obligatorio.");
  else if (tel.length > MAX_TELEFONO) falla("p-telefono", "El teléfono no puede superar los 20 caracteres.");

  const dir = $("p-direccion").value.trim();
  if (!dir) falla("p-direccion", "La dirección es obligatoria.");
  else if (dir.length > MAX_DIRECCION) falla("p-direccion", "La dirección no puede superar los 150 caracteres.");

  if (!valido) {
    const primero = $("form-proveedor").querySelector(".campo-error");
    if (primero) primero.focus();
  }
  return valido;
}

function leerFormulario() {
  return {
    nombreProveedor: $("p-nombre").value.trim(),
    telefono: $("p-telefono").value.trim(),
    direccion: $("p-direccion").value.trim()
  };
}

/* ---------- POST / PUT ---------- */
async function guardarProveedor() {
  if (!validarFormulario()) return;
  const btn = $("btn-guardar-proveedor");
  if (btn.disabled) return;
  const datos = leerFormulario();
  const editando = proveedorEnEdicion !== null;

  if (editando) {
    const p = proveedores.find(x => x.idProveedor === proveedorEnEdicion);
    const filas = Confirmar.cambios(
      { n: p.nombreProveedor, t: p.telefono, d: p.direccion },
      { n: datos.nombreProveedor, t: datos.telefono, d: datos.direccion },
      { n: "Nombre", t: "Teléfono", d: "Dirección" });
    if (!filas.length) { mostrarToast("No hay cambios para guardar."); return; }
    const ok = await Confirmar.pedir({ titulo: "Confirmar edición", mensaje: "Se modificará el proveedor <strong>" + esc(p.nombreProveedor) + "</strong>. ¿Desea guardar los cambios?", filas, textoConfirmar: "Sí, guardar cambios" });
    if (!ok) return;
  }

  bloquearBoton(btn, "Guardando...");
  const peticion = editando
    ? apiProveedores.actualizar(proveedorEnEdicion, { ...datos, idProveedor: proveedorEnEdicion }, proveedores.find(p => p.idProveedor === proveedorEnEdicion))  // PUT
    : apiProveedores.crear(datos);                                                                   // POST

  peticion
    .then(guardado => {
      if (editando) {
        const idx = proveedores.findIndex(p => p.idProveedor === guardado.idProveedor);
        if (idx !== -1) proveedores[idx] = guardado;
      } else {
        proveedores.push(guardado);
      }
      cerrarModal("overlay-proveedor");
      renderTabla();
      mostrarToast(editando ? "Proveedor actualizado correctamente." : "Proveedor creado correctamente.");
    })
    .catch(err => {
      const mapa = { nombreproveedor: "p-nombre", telefono: "p-telefono", direccion: "p-direccion" };
      Object.keys((err && err.errores) || {}).forEach(c => {
        const id = mapa[c.toLowerCase()];
        if (id) marcarError($(id), err.errores[c][0]);
      });
      mostrarToast(Api.mensajeError(err, "No fue posible guardar el proveedor. Intente de nuevo."), "error");
    })
    .finally(() => liberarBoton(btn));
}

/* ---------- DELETE ---------- */
function abrirEliminar(id) {
  const p = proveedores.find(x => x.idProveedor === id);
  if (!p) return;
  proveedorAEliminar = id;
  $("texto-eliminar").innerHTML = `¿Está seguro de que desea eliminar a <strong>${esc(p.nombreProveedor)}</strong>?`;
  abrirModal("overlay-eliminar");
}

function confirmarEliminacion() {
  if (proveedorAEliminar === null) return;
  const btn = $("btn-confirmar-eliminar");
  if (btn.disabled) return;
  const id = proveedorAEliminar;

  bloquearBoton(btn, "Eliminando...");
  apiProveedores.eliminar(id, proveedores.find(p => p.idProveedor === id))
    .then(() => {
      proveedores = proveedores.filter(p => p.idProveedor !== id);
      cerrarModal("overlay-eliminar");
      renderTabla();
      mostrarToast("Proveedor eliminado correctamente.");
    })
    .catch(err => {
      cerrarModal("overlay-eliminar");
      mostrarToast(
        err && err.status === 409 ? "No es posible eliminar este proveedor porque tiene compras asociadas."
        : Api.mensajeError(err, "No fue posible eliminar el proveedor. Intente de nuevo."),
        "error"
      );
    })
    .finally(() => { liberarBoton(btn); proveedorAEliminar = null; });
}
