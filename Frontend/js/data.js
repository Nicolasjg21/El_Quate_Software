/**
 * @file data.js
 * @description Utilidades compartidas: formato de moneda y fecha, escape de HTML y
 *   ventana de confirmación reutilizable.
 * @exports formatoCOP, horaActual, fechaHoy, esc, Confirmar
 */

/**
 * Formatea un valor en pesos colombianos, sin decimales: "$ 12.500".
 * @param {number} valor
 * @returns {string}
 */
function formatoCOP(valor) {
  return "$ " + Math.round(Number(valor) || 0).toLocaleString("es-CO");
}

/** Hora local "HH:mm". */
function horaActual() {
  const ahora = new Date();
  return ahora.getHours().toString().padStart(2, "0") + ":" + ahora.getMinutes().toString().padStart(2, "0");
}

/** Fecha local "D/M/AAAA" para el encabezado. */
function fechaHoy() {
  const ahora = new Date();
  return `${ahora.getDate()}/${ahora.getMonth() + 1}/${ahora.getFullYear()}`;
}

/**
 * Escapa texto para insertarlo como HTML. Todo dato proveniente del servidor debe pasar por aquí
 * antes de usarse con innerHTML.
 * @param {*} valor
 * @returns {string}
 */
function esc(valor) {
  return String(valor === undefined || valor === null ? "" : valor)
    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

/**
 * Ventana de confirmación previa a crear, editar o eliminar.
 *   Confirmar.pedir({ titulo, mensaje, filas: [[etiqueta, valorHTML]], textoConfirmar, peligro }) -> Promise<boolean>
 *   Confirmar.cambios(antes, despues, etiquetas) -> filas con los campos modificados
 * "mensaje" y los valores de "filas" se insertan como HTML: deben venir escapados.
 */
const Confirmar = (function () {
  let overlay = null, resolver = null;

  function construir() {
    if (overlay) return;
    overlay = document.createElement("div");
    overlay.className = "overlay";
    overlay.id = "overlay-confirmar";
    overlay.style.zIndex = "4000";
    overlay.innerHTML =
      '<div class="modal" role="alertdialog" aria-modal="true" aria-labelledby="confirmar-titulo">' +
      '<div class="modal-header"><p class="modal-titulo" id="confirmar-titulo"></p></div>' +
      '<div class="modal-cuerpo"><p id="confirmar-mensaje"></p><div id="confirmar-filas" style="margin-top:12px;display:flex;flex-direction:column;gap:6px;font-size:13px"></div></div>' +
      '<div class="modal-pie"><button type="button" class="btn btn--secundario" id="confirmar-cancelar">Cancelar</button>' +
      '<button type="button" class="btn btn--primario" id="confirmar-aceptar">Confirmar</button></div></div>';
    document.body.appendChild(overlay);
    overlay.querySelector("#confirmar-cancelar").addEventListener("click", () => cerrar(false));
    overlay.querySelector("#confirmar-aceptar").addEventListener("click", () => cerrar(true));
    overlay.addEventListener("click", (e) => { if (e.target === overlay) cerrar(false); });
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && overlay.classList.contains("overlay--visible")) cerrar(false);
    }, true);
  }

  function cerrar(valor) {
    overlay.classList.remove("overlay--visible");
    const r = resolver; resolver = null;
    if (r) r(valor);
  }

  function pedir(op) {
    construir();
    if (resolver) cerrar(false);
    overlay.querySelector("#confirmar-titulo").textContent = op.titulo || "Confirmar acción";
    overlay.querySelector("#confirmar-mensaje").innerHTML = op.mensaje || "";
    overlay.querySelector("#confirmar-filas").innerHTML = (op.filas || []).map((f) =>
      '<div style="display:flex;justify-content:space-between;gap:16px;border-bottom:1px solid var(--borde,#e5e5e5);padding-bottom:4px">' +
      '<span style="color:var(--texto-secundario,#666)">' + esc(f[0]) + '</span><strong style="text-align:right;word-break:break-word">' + f[1] + '</strong></div>').join("");
    const ok = overlay.querySelector("#confirmar-aceptar");
    ok.textContent = op.textoConfirmar || "Confirmar";
    ok.style.backgroundColor = op.peligro ? "var(--rojo-texto,#D8432B)" : "";
    overlay.classList.add("overlay--visible");
    overlay.querySelector("#confirmar-cancelar").focus();
    return new Promise((res) => { resolver = res; });
  }

  /**
   * Genera filas "antes → después" solo para los campos que cambiaron.
   * @param {object} antes
   * @param {object} despues
   * @param {Object<string,string>} etiquetas Campo → etiqueta visible.
   * @returns {Array<[string,string]>}
   */
  function cambios(antes, despues, etiquetas) {
    const filas = [];
    Object.keys(etiquetas).forEach((k) => {
      if (String(antes[k] === undefined || antes[k] === null ? "" : antes[k]) !== String(despues[k] === undefined || despues[k] === null ? "" : despues[k])) {
        filas.push([etiquetas[k], esc(antes[k] === "" || antes[k] == null ? "—" : antes[k]) + " &rarr; " + esc(despues[k] === "" || despues[k] == null ? "—" : despues[k])]);
      }
    });
    return filas;
  }

  return { pedir, cambios };
})();
