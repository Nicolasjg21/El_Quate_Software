/**
 * @file historiales.js
 * @description Módulo Historiales: órdenes pagadas y compras realizadas.
 *   - Filtros por período (hoy, ayer, últimos 7 y 30 días, semana, mes, mes anterior, año o rango)
 *     y filtros avanzados por mesa, quién atendió, método de pago, categoría, producto, montos y hora.
 *   - Exportación de reportes CSV con los mismos filtros aplicados en la tabla.
 *   - Consulta y reimpresión del comprobante de cada orden.
 *   - Registro de compras (requiere "inventario.gestionar"; la pestaña Compras también lo requiere).
 *   Los filtros se aplican en el navegador sobre los datos obtenidos de la API.
 * @requires api-negocio.js, historial-data.js, comprobante.js, data.js, auth.js
 */

const HIST_ICONO_OJO = `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
  <path d="M1.5 12S5.6 5 12 5s10.5 7 10.5 7-4.1 7-10.5 7-10.5-7-10.5-7Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="12" cy="12" r="3.1" stroke="currentColor" stroke-width="1.8"/>
</svg>`;

/** Estado de la pantalla: registros cargados, filtrados y filtros vigentes por pestaña. */
let histEstado = {
  ordenes: { filtradas: [], todas: [], filtros: null },
  compras: { filtradas: [], todas: [], filtros: null },
  ordenAbierta: null,
  comprobanteAbierto: null,
  lineasCompra: [],
  contadorLinea: 1,
  catalogo: [],
  proveedores: [],
  puedeCompras: false,
  guardando: false
};

const $ = (id) => document.getElementById(id);

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  configurarPestañas();
  configurarOrdenes();
  configurarCompras();
  configurarModalCompra();
  $("btn-descargar-reporte").addEventListener("click", descargarReporte);

  Auth.listo.then(() => {
    histEstado.puedeCompras = Auth.tiene("inventario.gestionar");
    $("tab-compras").hidden = !histEstado.puedeCompras;
    $("panel-compras").hidden = !histEstado.puedeCompras;
    if (histEstado.puedeCompras) {
      $("subtitulo-historiales").textContent = "Consulta, filtra y exporta las órdenes pagadas y las compras registradas.";
      cargarCompras();
    }
    cargarOrdenes();
  });
});

/* ==================== Utilidades generales ==================== */
function abrirModal(id) { $(id).classList.add("overlay--visible"); }
function cerrarModal(id) { $(id).classList.remove("overlay--visible"); }

document.addEventListener("click", (e) => {
  if (e.target.classList && e.target.classList.contains("overlay") && e.target.classList.contains("overlay--visible")) {
    cerrarModal(e.target.id);
  }
});

document.addEventListener("keydown", (e) => {
  if (e.key === "Escape") {
    document.querySelectorAll(".overlay--visible").forEach(overlay => cerrarModal(overlay.id));
    ocultarSugerencias();
  }
});

document.querySelectorAll("[data-cerrar]").forEach(btn => {
  btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
});

function mostrarToast(mensaje, esError) {
  const toast = $("toast");
  toast.textContent = mensaje;
  toast.classList.toggle("toast--error", !!esError);
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), esError ? 4500 : 2400);
}

/** Clase de color según el método de pago; los métodos sin estilo propio usan el neutro. */
function claseMetodoPago(nombre) {
  const n = histNormalizar(nombre);
  if (n.startsWith("efectivo")) return "efectivo";
  if (n.startsWith("tarjeta")) return "tarjeta";
  if (n.startsWith("billetera")) return "billetera";
  return "otro";
}

function badgeMetodoPago(nombre) {
  return `<span class="metodo-pago metodo-pago--${claseMetodoPago(nombre)}"><span class="metodo-pago-punto"></span>${esc(nombre || "—")}</span>`;
}

/**
 * Descarga un archivo CSV en UTF-8 con BOM.
 * Usa ";" como separador porque es el separador de lista de Excel en español (Colombia).
 * @param {string} nombreArchivo
 * @param {string[]} encabezados
 * @param {Array<Array<*>>} filas
 */
function descargarCSV(nombreArchivo, encabezados, filas) {
  const escapar = (valor) => `"${String(valor === undefined || valor === null ? "" : valor).replace(/"/g, '""')}"`;
  const lineas = [encabezados.map(escapar).join(";"), ...filas.map(fila => fila.map(escapar).join(";"))];
  const contenido = "\ufeff" + lineas.join("\r\n");
  const blob = new Blob([contenido], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement("a");
  enlace.href = url;
  enlace.download = nombreArchivo;
  document.body.appendChild(enlace);
  enlace.click();
  document.body.removeChild(enlace);
  URL.revokeObjectURL(url);
}

function filaMensaje(colspan, texto) {
  return `<tr><td colspan="${colspan}" class="col-centro">${esc(texto)}</td></tr>`;
}

function filaEstadoVacio(colspan, contextoLimpiar) {
  return `
    <tr>
      <td colspan="${colspan}">
        <div class="estado-vacio">
          <span class="estado-vacio-icono" aria-hidden="true">&#128269;</span>
          <span class="estado-vacio-titulo">No se encontraron registros.</span>
          <span class="estado-vacio-texto">Intenta ajustar la búsqueda o el período seleccionado.</span>
          <button class="btn btn--secundario" data-limpiar="${contextoLimpiar}" style="margin-top:6px;">Limpiar filtros</button>
        </div>
      </td>
    </tr>`;
}

/* ==================== Pestañas ==================== */
function configurarPestañas() {
  const tabOrdenes = $("tab-ordenes");
  const tabCompras = $("tab-compras");
  const panelOrdenes = $("panel-ordenes");
  const panelCompras = $("panel-compras");

  function activar(tab) {
    const esOrdenes = tab === "ordenes";
    tabOrdenes.classList.toggle("historial-tab--activo", esOrdenes);
    tabCompras.classList.toggle("historial-tab--activo", !esOrdenes);
    tabOrdenes.setAttribute("aria-selected", String(esOrdenes));
    tabCompras.setAttribute("aria-selected", String(!esOrdenes));
    panelOrdenes.classList.toggle("historial-panel--activo", esOrdenes);
    panelCompras.classList.toggle("historial-panel--activo", !esOrdenes);
  }

  tabOrdenes.addEventListener("click", () => activar("ordenes"));
  tabCompras.addEventListener("click", () => { if (histEstado.puedeCompras) activar("compras"); });
}

/* ==================== Filtros ==================== */

/**
 * Lee el período, la búsqueda y los filtros avanzados de una pestaña.
 * @param {"ordenes"|"compras"} t
 * @returns {object} Filtros normalizados, incluido el rango de fechas.
 */
function leerFiltros(t) {
  const periodo = $("periodo-" + t).value;
  const rango = histRangoPeriodo(periodo, $("desde-" + t).value, $("hasta-" + t).value);
  const v = (id) => ($(id) ? $(id).value.trim() : "");
  const n = (id) => { const x = v(id); return x === "" ? null : Number(x); };
  const base = { periodo, rango, busqueda: histNormalizar($("buscar-" + t).value) };
  if (t === "ordenes") {
    return Object.assign(base, {
      mesa: v("f-ord-mesa"), mesero: v("f-ord-mesero"), metodo: v("f-ord-metodo"),
      categoria: v("f-ord-categoria"), producto: v("f-ord-producto"),
      min: n("f-ord-min"), max: n("f-ord-max"), hDesde: v("f-ord-hdesde"), hHasta: v("f-ord-hhasta")
    });
  }
  return Object.assign(base, {
    proveedor: v("f-cmp-proveedor"), categoria: v("f-cmp-categoria"), producto: v("f-cmp-producto"),
    min: n("f-cmp-min"), max: n("f-cmp-max")
  });
}

const enRango = (fecha, r) => (!r.desde || fecha >= r.desde) && (!r.hasta || fecha <= r.hasta);
const minutosDe = (txt) => { const [h, m] = txt.split(":").map(Number); return h * 60 + m; };

/** Indica si la hora de una fecha está en la franja indicada. Admite franjas que cruzan la medianoche. */
function enFranja(fecha, desde, hasta) {
  if (!desde && !hasta) return true;
  const x = fecha.getHours() * 60 + fecha.getMinutes();
  const d = desde ? minutosDe(desde) : 0, h = hasta ? minutosDe(hasta) : 1439;
  return d <= h ? x >= d && x <= h : x >= d || x <= h;
}

/** Descripción legible de los filtros activos, para el resumen y el reporte. */
function describirFiltros(t, f) {
  const partes = [`Período: ${histEtiquetaPeriodo(f.periodo, f.rango)}`];
  const texto = (id) => { const s = $(id); return s && s.selectedIndex > 0 ? s.options[s.selectedIndex].text : ""; };
  if (f.busqueda) partes.push(`Búsqueda: "${$("buscar-" + t).value.trim()}"`);
  if (t === "ordenes") {
    if (f.mesa) partes.push(`Mesa: ${texto("f-ord-mesa")}`);
    if (f.mesero) partes.push(`Atendió: ${texto("f-ord-mesero")}`);
    if (f.metodo) partes.push(`Método de pago: ${f.metodo}`);
    if (f.categoria) partes.push(`Categoría: ${f.categoria}`);
    if (f.producto) partes.push(`Producto: ${texto("f-ord-producto")}`);
    if (f.min !== null) partes.push(`Total desde ${formatoCOP(f.min)}`);
    if (f.max !== null) partes.push(`Total hasta ${formatoCOP(f.max)}`);
    if (f.hDesde || f.hHasta) partes.push(`Hora de pago: ${f.hDesde || "00:00"} a ${f.hHasta || "23:59"}`);
  } else {
    if (f.proveedor) partes.push(`Proveedor: ${texto("f-cmp-proveedor")}`);
    if (f.categoria) partes.push(`Categoría: ${f.categoria}`);
    if (f.producto) partes.push(`Producto: ${texto("f-cmp-producto")}`);
    if (f.min !== null) partes.push(`Costo desde ${formatoCOP(f.min)}`);
    if (f.max !== null) partes.push(`Costo hasta ${formatoCOP(f.max)}`);
  }
  return partes;
}

function contarFiltrosAvanzados(t) {
  return [...$("filtros-" + t).querySelectorAll("select, input")].filter(el => el.value !== "").length;
}

function actualizarIndicadorFiltros(t, f) {
  const n = contarFiltrosAvanzados(t);
  const c = $("contador-filtros-" + t);
  c.textContent = n;
  c.hidden = n === 0;
  $("resumen-filtros-" + t).textContent = describirFiltros(t, f).join(" · ");
}

/** Conecta los controles de filtro de una pestaña con su función de dibujo. */
function configurarFiltros(t, render) {
  $("buscar-" + t).addEventListener("input", render);
  $("periodo-" + t).addEventListener("change", () => {
    $("rango-" + t).hidden = $("periodo-" + t).value !== "rango";
    render();
  });
  ["desde-", "hasta-"].forEach(p => $(p + t).addEventListener("change", render));
  $("filtros-" + t).addEventListener("input", render);
  $("filtros-" + t).addEventListener("change", render);
  const btn = $("btn-mas-filtros-" + t);
  btn.addEventListener("click", () => {
    const panel = $("filtros-" + t);
    panel.hidden = !panel.hidden;
    btn.setAttribute("aria-expanded", String(!panel.hidden));
  });
  $("filtros-" + t).querySelector(`[data-limpiar="${t}"]`).addEventListener("click", () => limpiarFiltros(t, render));
}

function limpiarFiltros(t, render) {
  $("buscar-" + t).value = "";
  $("periodo-" + t).value = "todos";
  $("rango-" + t).hidden = true;
  $("desde-" + t).value = "";
  $("hasta-" + t).value = "";
  $("filtros-" + t).querySelectorAll("select, input").forEach(el => { el.value = ""; });
  render();
  mostrarToast("Filtros restablecidos");
}

/** Llena una lista desplegable conservando la selección actual si sigue disponible. */
function llenarSelect(id, opciones, etiquetaTodos) {
  const s = $(id);
  const previo = s.value;
  s.innerHTML = `<option value="">${etiquetaTodos || "Todos"}</option>` +
    opciones.map(o => `<option value="${esc(o.valor)}">${esc(o.texto)}</option>`).join("");
  if (opciones.some(o => String(o.valor) === previo)) s.value = previo;
}

/** Opciones únicas { valor, texto } a partir de una lista, ordenadas alfabéticamente. */
const unicos = (lista, clave, texto, orden) => {
  const m = new Map();
  lista.forEach(x => { const k = clave(x); if (k !== undefined && k !== null && k !== "" && !m.has(String(k))) m.set(String(k), texto(x)); });
  return [...m.entries()].map(([valor, t]) => ({ valor, texto: t }))
    .sort(orden || ((a, b) => String(a.texto).localeCompare(String(b.texto), "es", { numeric: true })));
};

/* ==================== Órdenes pagadas ==================== */
function configurarOrdenes() {
  configurarFiltros("ordenes", renderOrdenes);
  $("btn-exportar-ordenes").addEventListener("click", () => abrirExportar("ordenes"));
  $("tabla-ordenes").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-orden]");
    if (btnVer) { abrirDetalleOrden(btnVer.dataset.verOrden); return; }
    if (e.target.closest("[data-limpiar='ordenes']")) limpiarFiltros("ordenes", renderOrdenes);
  });
  $("btn-ver-comprobante").addEventListener("click", () => {
    if (histEstado.ordenAbierta) abrirComprobante(histEstado.ordenAbierta);
  });
  $("btn-imprimir-comprobante").addEventListener("click", () => {
    if (histEstado.comprobanteAbierto) Comprobante.imprimir(histEstado.comprobanteAbierto);
  });
}

function cargarOrdenes() {
  $("tabla-ordenes").innerHTML = filaMensaje(7, "Cargando órdenes...");
  return Negocio.ordenesConItems()
    .then(({ ordenes, productos }) => {
      histEstado.ordenes.todas = ordenes;
      histEstado.catalogo = productos;
      poblarFiltrosOrdenes();
      renderOrdenes();
    })
    .catch((err) => {
      $("tabla-ordenes").innerHTML = filaMensaje(7, Api.mensajeError(err, "No fue posible cargar las órdenes. Intente de nuevo."));
    });
}

function poblarFiltrosOrdenes() {
  const o = histEstado.ordenes.todas;
  const items = o.flatMap(x => x.items);
  llenarSelect("f-ord-mesa", unicos(o, x => x.mesa, x => `Mesa ${x.mesa}`), "Todas");
  llenarSelect("f-ord-mesero", unicos(o, x => x.idUsuario, x => x.mesero));
  llenarSelect("f-ord-metodo", unicos(o, x => x.metodoPago !== "—" ? x.metodoPago : null, x => x.metodoPago));
  llenarSelect("f-ord-categoria", unicos(items, x => x.categoria, x => x.categoria), "Todas");
  llenarSelect("f-ord-producto", unicos(items, x => x.idProducto, x => x.nombre));
}

function textoBusquedaOrden(orden, fecha) {
  return histNormalizar([
    orden.id, orden.mesa, orden.mesero, histFormatoFechaHora(fecha), histFormatoFecha(fecha),
    orden.estado, orden.metodoPago, orden.total, formatoCOP(orden.total),
    orden.items.map(i => i.nombre).join(" ")
  ].join(" "));
}

/**
 * Aplica los filtros a las órdenes. El período se evalúa sobre la fecha de pago.
 * Los filtros de producto y categoría conservan las órdenes que contienen al menos una línea coincidente.
 */
function filtrarOrdenes(f) {
  return histEstado.ordenes.todas.filter(o => {
    const fecha = new Date(o.fechaHora);
    if (!enRango(fecha, f.rango)) return false;
    if (f.mesa && String(o.mesa) !== f.mesa) return false;
    if (f.mesero && String(o.idUsuario) !== f.mesero) return false;
    if (f.metodo && o.metodoPago !== f.metodo) return false;
    if (f.categoria && !o.items.some(i => i.categoria === f.categoria)) return false;
    if (f.producto && !o.items.some(i => String(i.idProducto) === f.producto)) return false;
    if (f.min !== null && o.total < f.min) return false;
    if (f.max !== null && o.total > f.max) return false;
    if (!enFranja(fecha, f.hDesde, f.hHasta)) return false;
    return !f.busqueda || textoBusquedaOrden(o, fecha).includes(f.busqueda);
  });
}

function renderOrdenes() {
  const f = leerFiltros("ordenes");
  const todas = histEstado.ordenes.todas;
  const filtradas = filtrarOrdenes(f);
  histEstado.ordenes.filtradas = filtradas;
  histEstado.ordenes.filtros = f;
  actualizarIndicadorFiltros("ordenes", f);
  const cuerpo = $("tabla-ordenes");

  if (!filtradas.length) {
    cuerpo.innerHTML = todas.length ? filaEstadoVacio(7, "ordenes") : filaMensaje(7, "Aún no hay órdenes pagadas.");
  } else {
    cuerpo.innerHTML = filtradas.map(orden => `
        <tr>
          <td>${esc(orden.id)}</td>
          <td class="col-centro">${esc(orden.mesa)}</td>
          <td>${esc(orden.mesero)}</td>
          <td>${histFormatoFechaHora(new Date(orden.fechaHora))}</td>
          <td class="col-derecha">${formatoCOP(orden.total)}</td>
          <td class="col-centro">${badgeMetodoPago(orden.metodoPago)}</td>
          <td class="col-centro">
            <button class="accion-ver" data-ver-orden="${esc(orden.id)}" title="Ver detalle de la orden" aria-label="Ver detalle de la orden ${esc(orden.id)}">${HIST_ICONO_OJO}</button>
          </td>
        </tr>`).join("");
  }

  const totalCOP = filtradas.reduce((a, o) => a + o.total, 0);
  $("tabla-ordenes-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} órdenes · Total cobrado: ${formatoCOP(totalCOP)}`;
}

/** Muestra el detalle de una orden: datos generales, líneas, subtotal, IVA y total. */
function abrirDetalleOrden(id) {
  const orden = histEstado.ordenes.todas.find(o => o.id === id);
  if (!orden) { mostrarToast("No fue posible cargar la orden seleccionada.", true); return; }
  histEstado.ordenAbierta = orden;

  $("detalle-orden-id").textContent = orden.id;
  $("detalle-orden-fecha").textContent = histFormatoFechaHora(new Date(orden.fechaHora));
  $("detalle-orden-mesa").textContent = orden.mesa;
  $("detalle-orden-mesero").textContent = orden.mesero;
  $("detalle-orden-estado").innerHTML = `<span class="pill pill--verde">${esc(orden.estado)}</span>`;
  $("detalle-orden-metodo").innerHTML = badgeMetodoPago(orden.metodoPago);
  $("detalle-orden-apertura").textContent = Comprobante.fechaHora(orden.apertura);
  $("detalle-orden-cierre").textContent = Comprobante.fechaHora(orden.cierre);

  $("detalle-orden-items").innerHTML = orden.items.length ? orden.items.map(item => `
    <tr>
      <td>${esc(item.nombre)}</td>
      <td class="col-centro">${item.cantidad}</td>
      <td class="col-derecha">${formatoCOP(item.precioUnitario)}</td>
      <td class="col-derecha">${formatoCOP(item.cantidad * item.precioUnitario)}</td>
    </tr>`).join("") : `<tr><td colspan="4" class="col-centro">Sin productos registrados.</td></tr>`;
  $("detalle-orden-subtotal").textContent = formatoCOP(orden.subtotal);
  $("detalle-orden-impuesto").textContent = formatoCOP(orden.iva);
  $("detalle-orden-total").textContent = formatoCOP(orden.total);
  abrirModal("overlay-detalle-orden");
}

/** Muestra el comprobante de una orden pagada para consulta o reimpresión. */
function abrirComprobante(orden) {
  histEstado.comprobanteAbierto = {
    numero: orden.idComprobante, borrador: false,
    mesa: orden.mesa, atendio: orden.mesero, cobro: "",
    apertura: orden.apertura, cierre: orden.cierre, metodo: orden.metodoPago,
    items: orden.items, subtotal: orden.subtotal, iva: orden.iva, total: orden.total, tasaIva: Negocio.IVA
  };
  $("comprobante-contenido").innerHTML = Comprobante.html(histEstado.comprobanteAbierto);
  abrirModal("overlay-comprobante");
}

/* ==================== Compras realizadas ==================== */
function configurarCompras() {
  configurarFiltros("compras", renderCompras);
  $("btn-exportar-compras").addEventListener("click", () => abrirExportar("compras"));
  $("tabla-compras").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-compra]");
    if (btnVer) { abrirDetalleCompra(btnVer.dataset.verCompra); return; }
    if (e.target.closest("[data-limpiar='compras']")) limpiarFiltros("compras", renderCompras);
  });
  $("btn-nueva-compra").addEventListener("click", abrirModalNuevaCompra);
}

function cargarCompras() {
  $("tabla-compras").innerHTML = filaMensaje(6, "Cargando compras...");
  return Negocio.listarCompras()
    .then((lista) => { histEstado.compras.todas = lista; poblarFiltrosCompras(); renderCompras(); })
    .catch((err) => {
      $("tabla-compras").innerHTML = filaMensaje(6, Api.mensajeError(err, "No fue posible cargar las compras. Intente de nuevo."));
    });
}

function poblarFiltrosCompras() {
  const c = histEstado.compras.todas;
  const items = c.flatMap(x => x.items);
  llenarSelect("f-cmp-proveedor", unicos(c, x => x.idProveedor, x => x.proveedor));
  llenarSelect("f-cmp-categoria", unicos(items, x => x.categoria, x => x.categoria), "Todas");
  llenarSelect("f-cmp-producto", unicos(items, x => x.idProducto, x => x.nombre));
}

function textoBusquedaCompra(compra, fecha) {
  const itemsTexto = compra.items.map(i => `${i.nombre} ${i.categoria}`).join(" ");
  const totalArticulos = compra.items.reduce((acc, it) => acc + it.cantidad, 0);
  return histNormalizar([
    compra.id, histFormatoFecha(fecha), compra.proveedor,
    compra.costoTotal, formatoCOP(compra.costoTotal), totalArticulos, itemsTexto
  ].join(" "));
}

/** Aplica los filtros a las compras. */
function filtrarCompras(f) {
  return histEstado.compras.todas.filter(c => {
    const fecha = new Date(c.fecha);
    if (!enRango(fecha, f.rango)) return false;
    if (f.proveedor && String(c.idProveedor) !== f.proveedor) return false;
    if (f.categoria && !c.items.some(i => i.categoria === f.categoria)) return false;
    if (f.producto && !c.items.some(i => String(i.idProducto) === f.producto)) return false;
    if (f.min !== null && c.costoTotal < f.min) return false;
    if (f.max !== null && c.costoTotal > f.max) return false;
    return !f.busqueda || textoBusquedaCompra(c, fecha).includes(f.busqueda);
  });
}

function renderCompras() {
  const f = leerFiltros("compras");
  const todas = histEstado.compras.todas;
  const filtradas = filtrarCompras(f);
  histEstado.compras.filtradas = filtradas;
  histEstado.compras.filtros = f;
  actualizarIndicadorFiltros("compras", f);
  const cuerpo = $("tabla-compras");

  if (!filtradas.length) {
    cuerpo.innerHTML = todas.length ? filaEstadoVacio(6, "compras") : filaMensaje(6, "Aún no hay compras registradas.");
  } else {
    cuerpo.innerHTML = filtradas.map(compra => {
      const totalArticulos = compra.items.reduce((acc, it) => acc + it.cantidad, 0);
      return `
        <tr>
          <td>${esc(compra.id)}</td>
          <td>${histFormatoFecha(new Date(compra.fecha))}</td>
          <td>${esc(compra.proveedor)}</td>
          <td class="col-centro">${totalArticulos}</td>
          <td class="col-derecha">${formatoCOP(compra.costoTotal)}</td>
          <td class="col-centro">
            <button class="accion-ver" data-ver-compra="${esc(compra.id)}" title="Ver detalle de la compra" aria-label="Ver detalle de la compra ${esc(compra.id)}">${HIST_ICONO_OJO}</button>
          </td>
        </tr>`;
    }).join("");
  }

  const totalCOP = filtradas.reduce((a, c) => a + c.costoTotal, 0);
  $("tabla-compras-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} compras · Costo total: ${formatoCOP(totalCOP)}`;
}

/* ==================== Reportes ====================
   Cada reporte recibe los documentos filtrados y devuelve { cab, filas }.
   Los montos se exportan en pesos enteros (COP) y los porcentajes con coma decimal. */
const pesos = (v) => Math.round(Number(v) || 0);
const porcentaje = (parte, total) => total ? (Math.round(parte / total * 1000) / 10).toString().replace(".", ",") : "0";
const fechaCorta = (d) => histFormatoFecha(new Date(d));
const fechaLarga = (d) => (d ? histFormatoFechaHora(new Date(d)) : "");
const diaISO = (d) => histFechaISO(new Date(d));

/**
 * Agrupa una lista y acumula valores por grupo.
 * @param {Array} lista
 * @param {Function} clave Obtiene la clave de agrupación.
 * @param {Function} inicial Crea el acumulador del grupo.
 * @param {Function} sumar Acumula un elemento en su grupo.
 * @returns {Array<object>}
 */
function agrupar(lista, clave, inicial, sumar) {
  const m = new Map();
  lista.forEach(x => {
    const k = clave(x);
    if (!m.has(k)) m.set(k, inicial(x));
    sumar(m.get(k), x);
  });
  return [...m.values()];
}

/** Líneas de los documentos que cumplen el filtro de producto y categoría, si existe. */
function lineasFiltradas(docs, f) {
  return docs.flatMap(d => d.items
    .filter(i => (!f.producto || String(i.idProducto) === f.producto) && (!f.categoria || i.categoria === f.categoria))
    .map(i => ({ doc: d, item: i })));
}

/**
 * Catálogo de reportes por pestaña.
 * Los reportes de detalle, por producto y por categoría cuentan solo las líneas filtradas;
 * los demás suman el documento completo.
 */
const REPORTES = {
  ordenes: [
    { id: "resumen", titulo: "Resumen de órdenes", desc: "Una fila por orden: comprobante, mesa, quién atendió, apertura, cierre, método, subtotal, IVA y total.",
      generar(docs) {
        const filas = docs.map(o => [o.id, o.idComprobante ? String(o.idComprobante).padStart(6, "0") : "", o.mesa, o.mesero,
          fechaLarga(o.apertura), fechaLarga(o.fechaHora), o.metodoPago, o.items.reduce((a, i) => a + i.cantidad, 0),
          pesos(o.subtotal), pesos(o.iva), pesos(o.total)]);
        const s = (k) => docs.reduce((a, o) => a + pesos(o[k]), 0);
        filas.push(["TOTAL", "", "", "", "", "", `${docs.length} órdenes`, docs.reduce((a, o) => a + o.items.reduce((b, i) => b + i.cantidad, 0), 0), s("subtotal"), s("iva"), s("total")]);
        return { cab: ["ID Orden", "N.º Comprobante", "Mesa", "Atendió", "Apertura", "Pago (cierre)", "Método de pago", "Unidades", "Subtotal (COP)", "IVA (COP)", "Total (COP)"], filas };
      } },
    { id: "detalle", titulo: "Detalle de productos vendidos", desc: "Una fila por producto de cada orden. Si filtró por producto o categoría, solo incluye esas líneas.",
      generar(docs, f) {
        const lineas = lineasFiltradas(docs, f);
        const filas = lineas.map(({ doc: o, item: i }) => [o.id, fechaLarga(o.fechaHora), o.mesa, o.mesero, o.metodoPago,
          i.nombre, i.categoria, i.cantidad, pesos(i.precioUnitario), pesos(i.cantidad * i.precioUnitario)]);
        filas.push(["TOTAL", "", "", "", "", "", "", lineas.reduce((a, l) => a + l.item.cantidad, 0), "", lineas.reduce((a, l) => a + pesos(l.item.cantidad * l.item.precioUnitario), 0)]);
        return { cab: ["ID Orden", "Fecha de pago", "Mesa", "Atendió", "Método de pago", "Producto", "Categoría", "Cantidad", "Precio unitario (COP)", "Subtotal sin IVA (COP)"], filas };
      } },
    { id: "producto", titulo: "Ventas por producto", desc: "Unidades vendidas, ventas sin IVA y participación de cada producto.",
      generar(docs, f) {
        const g = agrupar(lineasFiltradas(docs, f), l => l.item.idProducto,
          l => ({ n: l.item.nombre, c: l.item.categoria, u: 0, v: 0, o: new Set() }),
          (a, l) => { a.u += l.item.cantidad; a.v += l.item.cantidad * l.item.precioUnitario; a.o.add(l.doc.id); });
        const total = g.reduce((a, x) => a + x.v, 0);
        g.sort((a, b) => b.v - a.v);
        const filas = g.map(x => [x.n, x.c, x.u, pesos(x.v), porcentaje(x.v, total), x.o.size]);
        filas.push(["TOTAL", "", g.reduce((a, x) => a + x.u, 0), pesos(total), "100", ""]);
        return { cab: ["Producto", "Categoría", "Unidades vendidas", "Ventas sin IVA (COP)", "Participación (%)", "Órdenes en que aparece"], filas };
      } },
    { id: "categoria", titulo: "Ventas por categoría", desc: "Unidades y ventas sin IVA agrupadas por categoría del catálogo.",
      generar(docs, f) {
        const g = agrupar(lineasFiltradas(docs, f), l => l.item.categoria, l => ({ c: l.item.categoria, u: 0, v: 0 }),
          (a, l) => { a.u += l.item.cantidad; a.v += l.item.cantidad * l.item.precioUnitario; });
        const total = g.reduce((a, x) => a + x.v, 0);
        g.sort((a, b) => b.v - a.v);
        const filas = g.map(x => [x.c, x.u, pesos(x.v), porcentaje(x.v, total)]);
        filas.push(["TOTAL", g.reduce((a, x) => a + x.u, 0), pesos(total), "100"]);
        return { cab: ["Categoría", "Unidades vendidas", "Ventas sin IVA (COP)", "Participación (%)"], filas };
      } },
    { id: "metodo", titulo: "Ventas por método de pago", desc: "Cuántas órdenes y cuánto dinero entró por cada método (cuadre de caja).", generar: (docs) => porDimension(docs, o => o.metodoPago, "Método de pago") },
    { id: "mesero", titulo: "Ventas por persona que atendió", desc: "Órdenes, total cobrado y ticket promedio por mesero.", generar: (docs) => porDimension(docs, o => o.mesero, "Atendió") },
    { id: "mesa", titulo: "Ventas por mesa", desc: "Órdenes, total y ticket promedio por mesa.", generar: (docs) => porDimension(docs, o => `Mesa ${o.mesa}`, "Mesa") },
    { id: "dia", titulo: "Ventas por día", desc: "Cierre diario: órdenes, subtotal, IVA, total y ticket promedio.",
      generar(docs) {
        const g = agrupar(docs, o => diaISO(o.fechaHora), o => ({ d: fechaCorta(o.fechaHora), k: diaISO(o.fechaHora), n: 0, s: 0, i: 0, t: 0 }),
          (a, o) => { a.n++; a.s += pesos(o.subtotal); a.i += pesos(o.iva); a.t += pesos(o.total); });
        g.sort((a, b) => a.k.localeCompare(b.k));
        const filas = g.map(x => [x.d, x.n, x.s, x.i, x.t, x.n ? Math.round(x.t / x.n) : 0]);
        const T = (k) => g.reduce((a, x) => a + x[k], 0);
        filas.push(["TOTAL", T("n"), T("s"), T("i"), T("t"), T("n") ? Math.round(T("t") / T("n")) : 0]);
        return { cab: ["Fecha", "Órdenes", "Subtotal (COP)", "IVA (COP)", "Total (COP)", "Ticket promedio (COP)"], filas };
      } },
    { id: "hora", titulo: "Ventas por franja horaria", desc: "Órdenes y total por hora del día: muestra las horas de mayor movimiento.",
      generar(docs) {
        const g = agrupar(docs, o => new Date(o.fechaHora).getHours(), o => ({ h: new Date(o.fechaHora).getHours(), n: 0, t: 0 }),
          (a, o) => { a.n++; a.t += pesos(o.total); });
        g.sort((a, b) => a.h - b.h);
        const total = g.reduce((a, x) => a + x.t, 0);
        const filas = g.map(x => [`${String(x.h).padStart(2, "0")}:00 - ${String(x.h).padStart(2, "0")}:59`, x.n, x.t, porcentaje(x.t, total)]);
        filas.push(["TOTAL", g.reduce((a, x) => a + x.n, 0), total, "100"]);
        return { cab: ["Franja horaria", "Órdenes", "Total (COP)", "Participación (%)"], filas };
      } }
  ],
  compras: [
    { id: "resumen", titulo: "Resumen de compras", desc: "Una fila por compra: fecha, proveedor, productos, unidades y costo total.",
      generar(docs) {
        const filas = docs.map(c => [c.id, fechaCorta(c.fecha), c.proveedor, c.items.length, c.items.reduce((a, i) => a + i.cantidad, 0), pesos(c.costoTotal)]);
        filas.push(["TOTAL", "", `${docs.length} compras`, "", docs.reduce((a, c) => a + c.items.reduce((b, i) => b + i.cantidad, 0), 0), docs.reduce((a, c) => a + pesos(c.costoTotal), 0)]);
        return { cab: ["ID Compra", "Fecha", "Proveedor", "Productos diferentes", "Unidades", "Costo total (COP)"], filas };
      } },
    { id: "detalle", titulo: "Detalle de productos comprados", desc: "Una fila por producto de cada compra. Si filtró por producto o categoría, solo incluye esas líneas.",
      generar(docs, f) {
        const lineas = lineasFiltradas(docs, f);
        const filas = lineas.map(({ doc: c, item: i }) => [c.id, fechaCorta(c.fecha), c.proveedor, i.nombre, i.categoria, i.cantidad, pesos(i.costoUnitario), pesos(i.cantidad * i.costoUnitario)]);
        filas.push(["TOTAL", "", "", "", "", lineas.reduce((a, l) => a + l.item.cantidad, 0), "", lineas.reduce((a, l) => a + pesos(l.item.cantidad * l.item.costoUnitario), 0)]);
        return { cab: ["ID Compra", "Fecha", "Proveedor", "Producto", "Categoría", "Cantidad", "Costo unitario (COP)", "Subtotal (COP)"], filas };
      } },
    { id: "producto", titulo: "Compras por producto", desc: "Unidades compradas, costo total y costo unitario promedio, mínimo y máximo de cada producto.",
      generar(docs, f) {
        const g = agrupar(lineasFiltradas(docs, f), l => l.item.idProducto,
          l => ({ n: l.item.nombre, c: l.item.categoria, u: 0, v: 0, min: Infinity, max: 0, k: new Set() }),
          (a, l) => { a.u += l.item.cantidad; a.v += l.item.cantidad * l.item.costoUnitario; a.min = Math.min(a.min, l.item.costoUnitario); a.max = Math.max(a.max, l.item.costoUnitario); a.k.add(l.doc.id); });
        g.sort((a, b) => b.v - a.v);
        const filas = g.map(x => [x.n, x.c, x.u, pesos(x.v), x.u ? pesos(x.v / x.u) : 0, pesos(x.min), pesos(x.max), x.k.size]);
        filas.push(["TOTAL", "", g.reduce((a, x) => a + x.u, 0), pesos(g.reduce((a, x) => a + x.v, 0)), "", "", "", ""]);
        return { cab: ["Producto", "Categoría", "Unidades compradas", "Costo total (COP)", "Costo unitario promedio (COP)", "Costo mínimo (COP)", "Costo máximo (COP)", "Compras en que aparece"], filas };
      } },
    { id: "proveedor", titulo: "Compras por proveedor", desc: "Número de compras, unidades, costo total y participación de cada proveedor.",
      generar(docs) {
        const g = agrupar(docs, c => c.idProveedor, c => ({ p: c.proveedor, n: 0, u: 0, v: 0 }),
          (a, c) => { a.n++; a.u += c.items.reduce((b, i) => b + i.cantidad, 0); a.v += pesos(c.costoTotal); });
        const total = g.reduce((a, x) => a + x.v, 0);
        g.sort((a, b) => b.v - a.v);
        const filas = g.map(x => [x.p, x.n, x.u, x.v, porcentaje(x.v, total)]);
        filas.push(["TOTAL", g.reduce((a, x) => a + x.n, 0), g.reduce((a, x) => a + x.u, 0), total, "100"]);
        return { cab: ["Proveedor", "Compras", "Unidades", "Costo total (COP)", "Participación (%)"], filas };
      } },
    { id: "categoria", titulo: "Compras por categoría", desc: "Unidades y costo agrupados por categoría del catálogo.",
      generar(docs, f) {
        const g = agrupar(lineasFiltradas(docs, f), l => l.item.categoria, l => ({ c: l.item.categoria, u: 0, v: 0 }),
          (a, l) => { a.u += l.item.cantidad; a.v += l.item.cantidad * l.item.costoUnitario; });
        const total = g.reduce((a, x) => a + x.v, 0);
        g.sort((a, b) => b.v - a.v);
        const filas = g.map(x => [x.c, x.u, pesos(x.v), porcentaje(x.v, total)]);
        filas.push(["TOTAL", g.reduce((a, x) => a + x.u, 0), pesos(total), "100"]);
        return { cab: ["Categoría", "Unidades", "Costo total (COP)", "Participación (%)"], filas };
      } },
    { id: "dia", titulo: "Compras por día", desc: "Compras, unidades y costo total de cada día.",
      generar(docs) {
        const g = agrupar(docs, c => diaISO(c.fecha), c => ({ d: fechaCorta(c.fecha), k: diaISO(c.fecha), n: 0, u: 0, v: 0 }),
          (a, c) => { a.n++; a.u += c.items.reduce((b, i) => b + i.cantidad, 0); a.v += pesos(c.costoTotal); });
        g.sort((a, b) => a.k.localeCompare(b.k));
        const filas = g.map(x => [x.d, x.n, x.u, x.v]);
        filas.push(["TOTAL", g.reduce((a, x) => a + x.n, 0), g.reduce((a, x) => a + x.u, 0), g.reduce((a, x) => a + x.v, 0)]);
        return { cab: ["Fecha", "Compras", "Unidades", "Costo total (COP)"], filas };
      } }
  ]
};

/** Reporte de órdenes agrupado por una dimensión (método de pago, persona que atendió o mesa). */
function porDimension(docs, clave, nombre) {
  const g = agrupar(docs, clave, o => ({ k: clave(o), n: 0, t: 0 }), (a, o) => { a.n++; a.t += pesos(o.total); });
  const total = g.reduce((a, x) => a + x.t, 0);
  g.sort((a, b) => b.t - a.t);
  const filas = g.map(x => [x.k, x.n, x.t, porcentaje(x.t, total), x.n ? Math.round(x.t / x.n) : 0]);
  const n = g.reduce((a, x) => a + x.n, 0);
  filas.push(["TOTAL", n, total, "100", n ? Math.round(total / n) : 0]);
  return { cab: [nombre, "Órdenes", "Total cobrado (COP)", "Participación (%)", "Ticket promedio (COP)"], filas };
}

let exportando = "ordenes";

/** Abre la ventana de exportación con el resumen de los filtros vigentes. */
function abrirExportar(t) {
  exportando = t;
  const datos = histEstado[t];
  const f = datos.filtros || leerFiltros(t);
  const n = datos.filtradas.length;
  $("titulo-exportar").textContent = t === "ordenes" ? "Exportar reporte de órdenes" : "Exportar reporte de compras";
  $("exportar-resumen").innerHTML =
    `<strong>${n} ${t === "ordenes" ? (n === 1 ? "orden" : "órdenes") : (n === 1 ? "compra" : "compras")}</strong> con los filtros actuales:<br>` +
    describirFiltros(t, f).map(esc).join(" · ") +
    (f.producto || f.categoria
      ? `<br><em>Con filtro de producto o categoría: los reportes de detalle, por producto y por categoría solo cuentan esas líneas; los demás suman ${t === "ordenes" ? "la orden" : "la compra"} completa que las contiene.</em>`
      : "");
  $("exportar-opciones").innerHTML = REPORTES[t].map((r, i) => `
    <label class="exportar-opcion">
      <input type="radio" name="tipo-reporte" value="${r.id}" ${i === 0 ? "checked" : ""}>
      <div><strong>${esc(r.titulo)}</strong><span>${esc(r.desc)}</span></div>
    </label>`).join("");
  $("btn-descargar-reporte").disabled = n === 0;
  abrirModal("overlay-exportar");
}

/** Genera y descarga el reporte seleccionado. El nombre del archivo incluye el tipo y el período. */
function descargarReporte() {
  const t = exportando;
  const datos = histEstado[t];
  const f = datos.filtros || leerFiltros(t);
  const elegido = document.querySelector('input[name="tipo-reporte"]:checked');
  const reporte = REPORTES[t].find(r => r.id === (elegido && elegido.value)) || REPORTES[t][0];
  if (!datos.filtradas.length) { mostrarToast("No hay registros para exportar con estos filtros.", true); return; }

  const { cab, filas } = reporte.generar(datos.filtradas, f);
  const desde = f.rango.desde ? histFechaISO(f.rango.desde) : "inicio";
  const hasta = f.rango.hasta ? histFechaISO(f.rango.hasta) : histFechaISO(new Date());
  const nombre = `${t}-${reporte.id}_${desde}_a_${hasta}.csv`;
  descargarCSV(nombre, cab, filas);
  cerrarModal("overlay-exportar");
  mostrarToast(`Reporte "${reporte.titulo}" descargado.`);
}

/** Muestra el detalle de una compra. */
function abrirDetalleCompra(id) {
  const compra = histEstado.compras.todas.find(c => c.id === id);
  if (!compra) { mostrarToast("No fue posible cargar la compra seleccionada.", true); return; }

  const totalUnidades = compra.items.reduce((acc, it) => acc + it.cantidad, 0);

  $("detalle-compra-id").textContent = compra.id;
  $("detalle-compra-fecha").textContent = histFormatoFecha(new Date(compra.fecha));
  $("detalle-compra-proveedor").textContent = compra.proveedor;
  $("detalle-compra-total-productos").textContent = compra.items.length;
  $("detalle-compra-costo-total").textContent = formatoCOP(compra.costoTotal);

  $("detalle-compra-items").innerHTML = compra.items.length ? compra.items.map(item => `
    <tr>
      <td>${esc(item.nombre)}</td>
      <td>${esc(item.categoria)}</td>
      <td class="col-centro">${item.cantidad}</td>
      <td class="col-derecha">${formatoCOP(item.costoUnitario)}</td>
      <td class="col-derecha">${formatoCOP(item.cantidad * item.costoUnitario)}</td>
    </tr>`).join("") : `<tr><td colspan="5" class="col-centro">Sin productos registrados.</td></tr>`;

  $("detalle-compra-productos-diferentes").textContent = compra.items.length;
  $("detalle-compra-total-unidades").textContent = totalUnidades;
  $("detalle-compra-total").textContent = formatoCOP(compra.costoTotal);

  abrirModal("overlay-detalle-compra");
}

/* ==================== Registro de compras ==================== */

/** Opciones de productos agrupadas por categoría. */
function opcionesProductoSelect(seleccionado) {
  const categorias = [...new Set(histEstado.catalogo.map(p => p.categoria))].sort((a, b) => a.localeCompare(b));
  let html = `<option value="">Seleccionar producto</option>`;
  categorias.forEach(cat => {
    html += `<optgroup label="${esc(cat)}">`;
    histEstado.catalogo.filter(p => p.categoria === cat).forEach(p => {
      html += `<option value="${p.idProducto}" ${p.idProducto === seleccionado ? "selected" : ""}>${esc(p.nombre)}</option>`;
    });
    html += `</optgroup>`;
  });
  return html;
}

function configurarModalCompra() {
  $("buscar-producto-compra").addEventListener("input", (e) => renderSugerenciasProducto(e.target.value));
  $("buscar-producto-compra").addEventListener("focus", (e) => {
    if (e.target.value.trim()) renderSugerenciasProducto(e.target.value);
  });

  document.addEventListener("click", (e) => {
    if (!e.target.closest(".buscador-producto")) ocultarSugerencias();
  });

  $("btn-agregar-otro-producto").addEventListener("click", () => agregarLineaCompra());

  $("tabla-productos-compra").addEventListener("input", manejarCambioLinea);
  $("tabla-productos-compra").addEventListener("change", manejarCambioLinea);
  $("tabla-productos-compra").addEventListener("click", (e) => {
    const btnQuitar = e.target.closest("[data-quitar-linea]");
    if (btnQuitar) quitarLineaCompra(btnQuitar.dataset.quitarLinea);
  });

  $("btn-guardar-compra").addEventListener("click", guardarCompra);
}

/** Abre el formulario de compra con productos activos y proveedores actualizados. */
async function abrirModalNuevaCompra() {
  if (!histEstado.puedeCompras) return;
  histEstado.lineasCompra = [];
  histEstado.contadorLinea = 1;

  $("input-proveedor-compra").value = "";
  $("input-fecha-compra").value = histFechaISO(new Date());
  $("input-fecha-compra").max = histFechaISO(new Date());
  $("buscar-producto-compra").value = "";
  ocultarSugerencias();
  errorCompra("");
  document.querySelectorAll("#overlay-nueva-compra .campo-error").forEach(el => el.classList.remove("campo-error"));

  const btn = $("btn-nueva-compra");
  btn.disabled = true;
  try {
    const [prods, provs] = await Promise.all([Negocio.productos(), Negocio.proveedores()]);
    histEstado.catalogo = prods.filter(p => p.activo);
    histEstado.proveedores = provs;
  } catch (err) {
    mostrarToast(Api.mensajeError(err, "No fue posible cargar productos y proveedores."), true);
    btn.disabled = false;
    return;
  }
  btn.disabled = false;

  $("input-proveedor-compra").innerHTML = `<option value="">Seleccionar proveedor</option>` +
    histEstado.proveedores.map(p => `<option value="${p.idProveedor}">${esc(p.nombreProveedor)}</option>`).join("");

  renderTablaCompra();
  abrirModal("overlay-nueva-compra");
}

function errorCompra(texto) {
  const el = $("error-compra");
  el.textContent = texto;
  el.hidden = !texto;
}

function renderSugerenciasProducto(query) {
  const contenedor = $("sugerencias-productos");
  const termino = histNormalizar(query);
  if (!termino) { ocultarSugerencias(); return; }

  const coincidencias = histEstado.catalogo.filter(p => histNormalizar(p.nombre).includes(termino)).slice(0, 8);

  if (!coincidencias.length) {
    contenedor.innerHTML = `<div class="sugerencias-vacio">No se encontraron productos.</div>`;
  } else {
    contenedor.innerHTML = coincidencias.map(p => `
      <div class="sugerencia-item" data-sugerencia="${p.idProducto}">
        <span>${esc(p.nombre)}</span>
        <span class="sugerencia-item-categoria">${esc(p.categoria)}</span>
      </div>`).join("");

    contenedor.querySelectorAll("[data-sugerencia]").forEach(item => {
      item.addEventListener("click", () => {
        const producto = histEstado.catalogo.find(p => p.idProducto === Number(item.dataset.sugerencia));
        agregarLineaCompra(producto);
        $("buscar-producto-compra").value = "";
        ocultarSugerencias();
      });
    });
  }
  contenedor.classList.add("sugerencias-productos--visible");
}

function ocultarSugerencias() {
  const contenedor = $("sugerencias-productos");
  if (contenedor) contenedor.classList.remove("sugerencias-productos--visible");
}

function agregarLineaCompra(producto) {
  // Un producto ya agregado suma una unidad en lugar de duplicar la línea.
  if (producto) {
    const existente = histEstado.lineasCompra.find(l => l.idProducto === producto.idProducto);
    if (existente) { existente.cantidad += 1; renderTablaCompra(); return; }
  }
  histEstado.lineasCompra.push({
    id: `linea-${histEstado.contadorLinea++}`,
    idProducto: producto ? producto.idProducto : 0,
    categoria: producto ? producto.categoria : "",
    cantidad: 1,
    costoUnitario: producto ? producto.costo : 0
  });
  renderTablaCompra();
}

function quitarLineaCompra(id) {
  histEstado.lineasCompra = histEstado.lineasCompra.filter(l => l.id !== id);
  renderTablaCompra();
}

function manejarCambioLinea(e) {
  const fila = e.target.closest("tr[data-linea-id]");
  if (!fila) return;
  const linea = histEstado.lineasCompra.find(l => l.id === fila.dataset.lineaId);
  const campo = e.target.dataset.campo;
  if (!linea || !campo) return;

  if (campo === "producto") {
    linea.idProducto = Number(e.target.value) || 0;
    const producto = histEstado.catalogo.find(p => p.idProducto === linea.idProducto);
    linea.categoria = producto ? producto.categoria : "";
    linea.costoUnitario = producto ? producto.costo : 0;
    renderTablaCompra();
    return;
  }
  if (campo === "cantidad") {
    const valor = parseInt(e.target.value, 10);
    linea.cantidad = isNaN(valor) ? 0 : valor;
  }
  if (campo === "costoUnitario") {
    const valor = parseFloat(e.target.value);
    linea.costoUnitario = isNaN(valor) ? 0 : valor;
  }
  actualizarSubtotalFila(linea);
  actualizarResumenCompra();
}

function actualizarSubtotalFila(linea) {
  const fila = document.querySelector(`tr[data-linea-id="${linea.id}"]`);
  const celda = fila && fila.querySelector(".col-subtotal");
  if (celda) celda.textContent = formatoCOP(Math.max(0, linea.cantidad) * Math.max(0, linea.costoUnitario));
}

function renderTablaCompra() {
  const cuerpo = $("tabla-productos-compra");
  const scrollTabla = cuerpo.closest(".tabla-scroll");
  const estadoVacio = $("estado-vacio-compra");

  if (!histEstado.lineasCompra.length) {
    cuerpo.innerHTML = "";
    scrollTabla.style.display = "none";
    estadoVacio.style.display = "flex";
  } else {
    scrollTabla.style.display = "";
    estadoVacio.style.display = "none";
    cuerpo.innerHTML = histEstado.lineasCompra.map(linea => `
      <tr data-linea-id="${linea.id}">
        <td><select data-campo="producto">${opcionesProductoSelect(linea.idProducto)}</select></td>
        <td><input type="text" value="${esc(linea.categoria)}" readonly placeholder="—"></td>
        <td class="col-cantidad"><input type="number" min="1" step="1" data-campo="cantidad" value="${linea.cantidad}"></td>
        <td class="col-costo">
          <div class="campo-prefijo"><span>$</span><input type="number" min="0" step="1" data-campo="costoUnitario" value="${linea.costoUnitario}"></div>
        </td>
        <td class="col-subtotal">${formatoCOP(Math.max(0, linea.cantidad) * Math.max(0, linea.costoUnitario))}</td>
        <td class="col-acciones">
          <button class="btn-quitar-fila" data-quitar-linea="${linea.id}" title="Eliminar producto" aria-label="Eliminar producto">&#128465;</button>
        </td>
      </tr>`).join("");
  }
  actualizarResumenCompra();
}

function actualizarResumenCompra() {
  const validas = histEstado.lineasCompra.filter(l => l.idProducto);
  $("resumen-productos-diferentes").textContent = validas.length;
  $("resumen-total-unidades").textContent = validas.reduce((a, l) => a + Math.max(0, l.cantidad), 0);
  $("resumen-costo-total").textContent = formatoCOP(validas.reduce((a, l) => a + Math.max(0, l.cantidad) * Math.max(0, l.costoUnitario), 0));
}

/**
 * Valida y registra la compra. Cantidades: enteros mayores que cero.
 * Costos: pesos enteros (COP) entre 0 y 99 999 999.
 */
async function guardarCompra() {
  if (histEstado.guardando || !histEstado.puedeCompras) return;
  document.querySelectorAll("#overlay-nueva-compra .campo-error").forEach(el => el.classList.remove("campo-error"));
  errorCompra("");

  const idProveedor = Number($("input-proveedor-compra").value);
  const fecha = $("input-fecha-compra").value;

  if (!idProveedor) { $("input-proveedor-compra").classList.add("campo-error"); errorCompra("Seleccione un proveedor."); return; }
  if (!fecha) { $("input-fecha-compra").classList.add("campo-error"); errorCompra("Ingrese una fecha de compra válida."); return; }

  const lineas = histEstado.lineasCompra.filter(l => l.idProducto);
  if (!lineas.length) { errorCompra("Agregue al menos un producto a la compra."); return; }

  let validas = true;
  lineas.forEach(l => {
    const fila = document.querySelector(`tr[data-linea-id="${l.id}"]`);
    const malaCant = !Number.isInteger(l.cantidad) || l.cantidad <= 0;
    const malCosto = !Number.isInteger(l.costoUnitario) || l.costoUnitario < 0 || l.costoUnitario > 99999999;
    if (fila) {
      fila.querySelector('[data-campo="cantidad"]').classList.toggle("campo-error", malaCant);
      fila.querySelector('[data-campo="costoUnitario"]').classList.toggle("campo-error", malCosto);
    }
    if (malaCant || malCosto) validas = false;
  });
  if (!validas) { errorCompra("Verifique las cantidades (enteros mayores a 0) y los costos (pesos COP, sin centavos)."); return; }

  histEstado.guardando = true;
  const btn = $("btn-guardar-compra");
  const textoBtn = btn.textContent;
  btn.disabled = true;
  btn.textContent = "Guardando...";
  try {
    const r = await Negocio.crearCompra({
      idProveedor, fecha,
      items: lineas.map(l => ({ idProducto: l.idProducto, cantidad: l.cantidad, costoUnitario: l.costoUnitario }))
    });
    cerrarModal("overlay-nueva-compra");
    await cargarCompras();
    mostrarToast(r.aviso.length ? "Compra registrada con avisos: " + r.aviso.join(" ") : "Compra registrada correctamente.", r.aviso.length > 0);
  } catch (err) {
    errorCompra(Api.mensajeError(err, "No fue posible registrar la compra. Intente de nuevo."));
  } finally {
    histEstado.guardando = false;
    btn.disabled = false;
    btn.textContent = textoBtn;
  }
}
