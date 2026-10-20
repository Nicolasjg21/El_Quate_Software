/**
 * @file admin-ui.js
 * @description Componentes de interfaz compartidos por las pantallas de administración
 *   (Usuarios, Proveedores, Auditoría): iconos, ventanas modales, avisos y errores de formulario.
 */

const ICONO_EDITAR = `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M17 3a2.8 2.8 0 0 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z"/></svg>`;
const ICONO_ELIMINAR = `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18"/><path d="M8 6V4h8v2"/><path d="M19 6l-1 14H6L5 6"/><path d="M10 11v6M14 11v6"/></svg>`;

/** Texto en minúsculas, sin tildes ni espacios extremos, para búsquedas. */
function normalizar(texto) {
  return String(texto ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
}

/** Muestra una ventana modal y enfoca su primer campo. */
function abrirModal(id) {
  const overlay = document.getElementById(id);
  overlay.classList.add("overlay--visible");
  const primero = overlay.querySelector("input, select");
  if (primero) setTimeout(() => primero.focus(), 30);
}
function cerrarModal(id) { document.getElementById(id).classList.remove("overlay--visible"); }

/** Cierra las ventanas con los botones [data-cerrar] y con la tecla Escape. */
function conectarCierreModales() {
  document.querySelectorAll("[data-cerrar]").forEach(btn => {
    btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
  });
  document.addEventListener("keydown", (e) => {
    if (e.key !== "Escape") return;
    document.querySelectorAll(".overlay--visible").forEach(o => o.classList.remove("overlay--visible"));
  });
}

/**
 * @param {string} mensaje
 * @param {"error"} [tipo]
 */
function mostrarToast(mensaje, tipo) {
  const toast = document.getElementById("toast");
  toast.textContent = mensaje;
  toast.classList.toggle("toast--error", tipo === "error");
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), tipo === "error" ? 3800 : 2400);
}

/** Marca un campo como inválido y muestra su mensaje. */
function marcarError(input, mensaje) {
  const campo = input.closest(".campo");
  const aviso = campo.querySelector(".campo-mensaje-error");
  input.classList.add("campo-error");
  input.setAttribute("aria-invalid", "true");
  aviso.textContent = mensaje;
  aviso.classList.add("campo-mensaje-error--visible");
}

function limpiarErrores(contenedor) {
  contenedor.querySelectorAll(".campo-error").forEach(i => { i.classList.remove("campo-error"); i.removeAttribute("aria-invalid"); });
  contenedor.querySelectorAll(".campo-mensaje-error--visible").forEach(a => a.classList.remove("campo-mensaje-error--visible"));
}

/** Retira el error de un campo en cuanto el usuario lo modifica. */
function limpiarErrorAlEscribir(contenedor) {
  contenedor.querySelectorAll("input, select").forEach(el => {
    const evento = el.tagName === "SELECT" ? "change" : "input";
    el.addEventListener(evento, () => {
      el.classList.remove("campo-error");
      el.removeAttribute("aria-invalid");
      const aviso = el.closest(".campo").querySelector(".campo-mensaje-error");
      if (aviso) aviso.classList.remove("campo-mensaje-error--visible");
    });
  });
}

/** Deshabilita el botón durante una operación y muestra un texto de progreso. */
function bloquearBoton(btn, textoProgreso) {
  btn.dataset.textoOriginal = btn.textContent;
  btn.textContent = textoProgreso;
  btn.disabled = true;
}
function liberarBoton(btn) {
  btn.textContent = btn.dataset.textoOriginal || btn.textContent;
  btn.disabled = false;
}
