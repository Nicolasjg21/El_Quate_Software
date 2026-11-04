/**
 * @file comprobante.js
 * @description Comprobante de pago: documento interno de control, no es factura electrónica.
 *   Se usa en Mesas, al registrar un cobro, y en Historiales, para consultar o reimprimir una orden.
 *   Los valores se expresan en pesos colombianos (COP).
 * @requires data.js
 * @exports window.Comprobante
 *
 * Estructura de datos esperada por html() e imprimir():
 *   {
 *     numero,                 idComprobante; null mientras es vista previa
 *     borrador,               true en la vista previa, antes de registrar el pago
 *     mesa, atendio, cobro,   textos
 *     apertura, cierre,       fechas (ISO o Date); cierre es null en la vista previa
 *     metodo,                 método con el que pagó el cliente
 *     items,                  [{ nombre, cantidad, precioUnitario }]
 *     subtotal, iva, total, tasaIva
 *   }
 */

(function () {
  "use strict";

  const pad = (n) => String(n).padStart(2, "0");

  /** Fecha y hora "DD/MM/AAAA HH:mm"; "—" si no hay valor. */
  function fechaHora(valor) {
    if (!valor) return "—";
    const d = valor instanceof Date ? valor : new Date(valor);
    if (isNaN(d)) return "—";
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  /** Número visible del comprobante: N.º 000123. */
  const numero = (n) => (n ? "N.º " + String(n).padStart(6, "0") : "Sin registrar");

  /**
   * @param {object} d Datos del comprobante.
   * @returns {string} HTML del comprobante.
   */
  function html(d) {
    const filas = (d.items || []).map((i) => `
      <tr>
        <td>${esc(i.nombre)}</td>
        <td class="cpb-centro">${esc(i.cantidad)}</td>
        <td class="cpb-derecha">${formatoCOP(i.precioUnitario)}</td>
        <td class="cpb-derecha">${formatoCOP(i.cantidad * i.precioUnitario)}</td>
      </tr>`).join("");
    const tasa = Math.round((d.tasaIva === undefined ? 0.19 : d.tasaIva) * 100);

    return `
      <article class="cpb${d.borrador ? " cpb--borrador" : ""}">
        <header class="cpb-cabecera">
          <div>
            <p class="cpb-negocio">El Cuate</p>
            <p class="cpb-tipo">Comprobante de pago</p>
          </div>
          <div class="cpb-numero">
            <span>${esc(numero(d.numero))}</span>
            ${d.borrador ? `<span class="cpb-sello">Vista previa</span>` : `<span class="cpb-sello cpb-sello--pagado">Pagado</span>`}
          </div>
        </header>

        <dl class="cpb-datos">
          <div><dt>Mesa</dt><dd>${esc(d.mesa)}</dd></div>
          <div><dt>Atendió</dt><dd>${esc(d.atendio || "—")}</dd></div>
          <div><dt>Apertura de la cuenta</dt><dd>${esc(fechaHora(d.apertura))}</dd></div>
          <div><dt>Cierre de la cuenta</dt><dd>${d.cierre ? esc(fechaHora(d.cierre)) : "Al registrar el pago"}</dd></div>
          <div><dt>Método de pago</dt><dd>${esc(d.metodo || (d.borrador ? "Por seleccionar" : "—"))}</dd></div>
          <div><dt>Cobró</dt><dd>${esc(d.cobro || "—")}</dd></div>
        </dl>

        <table class="cpb-tabla">
          <thead>
            <tr><th>Producto</th><th class="cpb-centro">Cant.</th><th class="cpb-derecha">Precio unit.</th><th class="cpb-derecha">Subtotal</th></tr>
          </thead>
          <tbody>${filas || `<tr><td colspan="4" class="cpb-centro">Sin productos.</td></tr>`}</tbody>
        </table>

        <div class="cpb-totales">
          <div><span>Subtotal</span><span>${formatoCOP(d.subtotal)}</span></div>
          <div><span>IVA (${tasa} %)</span><span>${formatoCOP(d.iva)}</span></div>
          <div class="cpb-total"><span>Total pagado (COP)</span><span>${formatoCOP(d.total)}</span></div>
        </div>

        <p class="cpb-nota">Documento interno de control. No es factura electrónica de venta.</p>
      </article>`;
  }

  /** Estilos de impresión, independientes de la hoja de estilos de la página. */
  const CSS_IMPRESION = `
    *{box-sizing:border-box;margin:0;padding:0}
    body{font-family:system-ui,-apple-system,"Segoe UI",Roboto,sans-serif;color:#1F2426;padding:18px;font-size:12px}
    .cpb{max-width:380px;margin:0 auto}
    .cpb-cabecera{display:flex;justify-content:space-between;gap:10px;border-bottom:2px solid #1F2426;padding-bottom:8px;margin-bottom:10px}
    .cpb-negocio{font-size:18px;font-weight:800}.cpb-tipo{font-size:11px;color:#555}
    .cpb-numero{text-align:right;display:flex;flex-direction:column;gap:4px;font-weight:700}
    .cpb-sello{font-size:10px;font-weight:700;text-transform:uppercase;border:1px solid #1F2426;padding:1px 6px;border-radius:4px;align-self:flex-end}
    .cpb-datos{display:grid;grid-template-columns:1fr 1fr;gap:6px 12px;margin-bottom:10px}
    .cpb-datos dt{font-size:10px;color:#555}.cpb-datos dd{font-weight:600}
    .cpb-tabla{width:100%;border-collapse:collapse;margin-bottom:10px}
    .cpb-tabla th{font-size:10px;text-align:left;border-bottom:1px solid #1F2426;padding:4px 2px}
    .cpb-tabla td{padding:4px 2px;border-bottom:1px dashed #bbb}
    .cpb-centro{text-align:center!important}.cpb-derecha{text-align:right!important;white-space:nowrap}
    .cpb-totales div{display:flex;justify-content:space-between;padding:2px 0}
    .cpb-total{font-size:15px;font-weight:800;border-top:2px solid #1F2426;margin-top:4px;padding-top:6px!important}
    .cpb-nota{margin-top:12px;font-size:10px;color:#555;text-align:center}
    @page{margin:10mm}`;

  /**
   * Imprime únicamente el comprobante a través de un iframe oculto, sin abrir ventanas emergentes.
   * @param {object} d Datos del comprobante.
   */
  function imprimir(d) {
    const marco = document.createElement("iframe");
    marco.setAttribute("aria-hidden", "true");
    marco.style.cssText = "position:fixed;right:0;bottom:0;width:0;height:0;border:0;visibility:hidden";
    document.body.appendChild(marco);
    const doc = marco.contentDocument || marco.contentWindow.document;
    doc.open();
    doc.write(`<!doctype html><html lang="es"><head><meta charset="utf-8"><title>Comprobante ${esc(numero(d.numero))}</title><style>${CSS_IMPRESION}</style></head><body>${html(d)}</body></html>`);
    doc.close();
    setTimeout(() => {
      try { marco.contentWindow.focus(); marco.contentWindow.print(); } catch (e) { }
      setTimeout(() => marco.remove(), 1000);
    }, 150);
  }

  window.Comprobante = { html, imprimir, fechaHora, numero };
})();
