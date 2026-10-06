/* ==========================================================================
   HISTORIALES.JS — Órdenes pagadas y compras (API real, vía Negocio)
   --------------------------------------------------------------------------
   · Órdenes pagadas: cualquier usuario con acceso a la página.
   · Compras: solo con permiso "inventario.gestionar" (administración).
   ========================================================================== */

const HIST_ICONO_OJO = `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
  <path d="M1.5 12S5.6 5 12 5s10.5 7 10.5 7-4.1 7-10.5 7-10.5-7-10.5-7Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="12" cy="12" r="3.1" stroke="currentColor" stroke-width="1.8"/>
</svg>`;

let histEstado = {
  ordenes: { busqueda: "", filtroTiempo: "todos", filtradas: [], todas: [] },
  compras: { busqueda: "", filtroTiempo: "todos", filtradas: [], todas: [] },
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

  Auth.listo.then(() => {
    histEstado.puedeCompras = Auth.tiene("inventario.gestionar");
    $("tab-compras").hidden = !histEstado.puedeCompras;
    $("panel-compras").hidden = !histEstado.puedeCompras;
    if (histEstado.puedeCompras) {
      $("subtitulo-historiales").textContent = "Consulta el historial completo de órdenes finalizadas y compras registradas.";
      cargarCompras();
    }
    cargarOrdenes();
  });
});

/* ==========================================================================
   UTILIDADES GENERALES
   ========================================================================== */
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

/** Clase de color según el nombre del método de pago (los nuevos usan el estilo neutro). */
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

function descargarCSV(nombreArchivo, encabezados, filas) {
  const escapar = (valor) => `"${String(valor === undefined || valor === null ? "" : valor).replace(/"/g, '""')}"`;
  const lineas = [encabezados.map(escapar).join(","), ...filas.map(fila => fila.map(escapar).join(","))];
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

/* ==========================================================================
   PESTAÑAS
   ========================================================================== */
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

/* ==========================================================================
   ÓRDENES PAGADAS
   ========================================================================== */
function configurarOrdenes() {
  $("buscar-ordenes").addEventListener("input", (e) => {
    histEstado.ordenes.busqueda = e.target.value;
    renderOrdenes();
  });

  document.querySelectorAll("[data-filtro-ordenes]").forEach(btn => {
    btn.addEventListener("click", () => {
      histEstado.ordenes.filtroTiempo = btn.dataset.filtroOrdenes;
      document.querySelectorAll("[data-filtro-ordenes]").forEach(b => b.classList.toggle("filtro-btn--activo", b === btn));
      renderOrdenes();
    });
  });

  $("btn-exportar-ordenes").addEventListener("click", () => {
    const filas = histEstado.ordenes.filtradas.map(o => [
      o.id, o.mesa, o.mesero, histFormatoFechaHora(new Date(o.fechaHora)),
      o.total, o.metodoPago, o.estado
    ]);
    if (!filas.length) { mostrarToast("No hay registros para exportar."); return; }
    descargarCSV("ordenes-pagadas.csv",
      ["ID Orden", "Mesa", "Mesero", "Fecha y Hora", "Total Pagado (COP)", "Método de Pago", "Estado"],
      filas);
    mostrarToast("Exportación de órdenes pagadas generada.");
  });

  $("tabla-ordenes").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-orden]");
    if (btnVer) { abrirDetalleOrden(btnVer.dataset.verOrden); return; }
    if (e.target.closest("[data-limpiar='ordenes']")) limpiarFiltrosOrdenes();
  });
}

function cargarOrdenes() {
  $("tabla-ordenes").innerHTML = filaMensaje(7, "Cargando órdenes...");
  return Negocio.ordenesPagadas()
    .then((lista) => { histEstado.ordenes.todas = lista; renderOrdenes(); })
    .catch((err) => {
      $("tabla-ordenes").innerHTML = filaMensaje(7, Api.mensajeError(err, "No fue posible cargar las órdenes. Intente de nuevo."));
    });
}

function limpiarFiltrosOrdenes() {
  histEstado.ordenes.busqueda = "";
  histEstado.ordenes.filtroTiempo = "todos";
  $("buscar-ordenes").value = "";
  document.querySelectorAll("[data-filtro-ordenes]").forEach(b => b.classList.toggle("filtro-btn--activo", b.dataset.filtroOrdenes === "todos"));
  renderOrdenes();
  mostrarToast("Filtros restablecidos");
}

function textoBusquedaOrden(orden, fecha) {
  return histNormalizar([
    orden.id, orden.mesa, orden.mesero, histFormatoFechaHora(fecha), histFormatoFecha(fecha),
    orden.estado, orden.metodoPago, orden.total, formatoCOP(orden.total)
  ].join(" "));
}

function renderOrdenes() {
  const ahora = new Date();
  const termino = histNormalizar(histEstado.ordenes.busqueda);
  const filtroTiempo = histEstado.ordenes.filtroTiempo;
  const todas = histEstado.ordenes.todas;

  const filtradas = todas.filter(orden => {
    const fecha = new Date(orden.fechaHora);
    let coincideTiempo = true;
    if (filtroTiempo === "hoy") coincideTiempo = histEsHoy(fecha, ahora);
    else if (filtroTiempo === "semana") coincideTiempo = histEsEstaSemana(fecha, ahora);
    else if (filtroTiempo === "mes") coincideTiempo = histEsEsteMes(fecha, ahora);
    if (!coincideTiempo) return false;
    return !termino || textoBusquedaOrden(orden, fecha).includes(termino);
  });

  histEstado.ordenes.filtradas = filtradas;
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

  $("tabla-ordenes-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} órdenes`;
}

/* ---------- Modal: Detalle de la Orden ---------- */
async function abrirDetalleOrden(id) {
  const orden = histEstado.ordenes.todas.find(o => o.id === id);
  if (!orden) { mostrarToast("No fue posible cargar la orden seleccionada.", true); return; }

  $("detalle-orden-id").textContent = orden.id;
  $("detalle-orden-fecha").textContent = histFormatoFechaHora(new Date(orden.fechaHora));
  $("detalle-orden-mesa").textContent = orden.mesa;
  $("detalle-orden-mesero").textContent = orden.mesero;
  $("detalle-orden-estado").innerHTML = `<span class="pill pill--verde">${esc(orden.estado)}</span>`;
  $("detalle-orden-metodo").innerHTML = badgeMetodoPago(orden.metodoPago);
  $("detalle-orden-items").innerHTML = `<tr><td colspan="4" class="col-centro">Cargando detalle...</td></tr>`;
  $("detalle-orden-subtotal").textContent = "—";
  $("detalle-orden-impuesto").textContent = "—";
  $("detalle-orden-total").textContent = formatoCOP(orden.total);
  abrirModal("overlay-detalle-orden");

  try {
    const items = await Negocio.itemsOrden(orden, histEstado.catalogo.length ? histEstado.catalogo : null);
    $("detalle-orden-items").innerHTML = items.length ? items.map(item => `
    <tr>
      <td>${esc(item.nombre)}</td>
      <td class="col-centro">${item.cantidad}</td>
      <td class="col-derecha">${formatoCOP(item.precioUnitario)}</td>
      <td class="col-derecha">${formatoCOP(item.cantidad * item.precioUnitario)}</td>
    </tr>`).join("") : `<tr><td colspan="4" class="col-centro">Sin productos registrados.</td></tr>`;
    const t = Negocio.totales(items);
    $("detalle-orden-subtotal").textContent = formatoCOP(t.subtotal);
    $("detalle-orden-impuesto").textContent = formatoCOP(t.iva);
  } catch (err) {
    $("detalle-orden-items").innerHTML = `<tr><td colspan="4" class="col-centro">${esc(Api.mensajeError(err, "No fue posible cargar el detalle."))}</td></tr>`;
  }
}

/* ==========================================================================
   COMPRAS REALIZADAS (solo administración)
   ========================================================================== */
function configurarCompras() {
  $("buscar-compras").addEventListener("input", (e) => {
    histEstado.compras.busqueda = e.target.value;
    renderCompras();
  });

  document.querySelectorAll("[data-filtro-compras]").forEach(btn => {
    btn.addEventListener("click", () => {
      histEstado.compras.filtroTiempo = btn.dataset.filtroCompras;
      document.querySelectorAll("[data-filtro-compras]").forEach(b => b.classList.toggle("filtro-btn--activo", b === btn));
      renderCompras();
    });
  });

  $("btn-exportar-compras").addEventListener("click", () => {
    const filas = histEstado.compras.filtradas.map(c => [
      c.id, histFormatoFecha(new Date(c.fecha)), c.proveedor,
      c.items.reduce((acc, it) => acc + it.cantidad, 0), c.costoTotal
    ]);
    if (!filas.length) { mostrarToast("No hay registros para exportar."); return; }
    descargarCSV("compras-realizadas.csv",
      ["ID Compra", "Fecha", "Proveedor", "Total de Artículos", "Costo Total (COP)"], filas);
    mostrarToast("Exportación de compras generada.");
  });

  $("tabla-compras").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-compra]");
    if (btnVer) { abrirDetalleCompra(btnVer.dataset.verCompra); return; }
    if (e.target.closest("[data-limpiar='compras']")) limpiarFiltrosCompras();
  });

  $("btn-nueva-compra").addEventListener("click", abrirModalNuevaCompra);
}

function cargarCompras() {
  $("tabla-compras").innerHTML = filaMensaje(6, "Cargando compras...");
  return Negocio.listarCompras()
    .then((lista) => { histEstado.compras.todas = lista; renderCompras(); })
    .catch((err) => {
      $("tabla-compras").innerHTML = filaMensaje(6, Api.mensajeError(err, "No fue posible cargar las compras. Intente de nuevo."));
    });
}

function limpiarFiltrosCompras() {
  histEstado.compras.busqueda = "";
  histEstado.compras.filtroTiempo = "todos";
  $("buscar-compras").value = "";
  document.querySelectorAll("[data-filtro-compras]").forEach(b => b.classList.toggle("filtro-btn--activo", b.dataset.filtroCompras === "todos"));
  renderCompras();
  mostrarToast("Filtros restablecidos");
}

function textoBusquedaCompra(compra, fecha) {
  const itemsTexto = compra.items.map(i => `${i.nombre} ${i.categoria}`).join(" ");
  const totalArticulos = compra.items.reduce((acc, it) => acc + it.cantidad, 0);
  return histNormalizar([
    compra.id, histFormatoFecha(fecha), compra.proveedor,
    compra.costoTotal, formatoCOP(compra.costoTotal), totalArticulos, itemsTexto
  ].join(" "));
}

function renderCompras() {
  const ahora = new Date();
  const termino = histNormalizar(histEstado.compras.busqueda);
  const filtroTiempo = histEstado.compras.filtroTiempo;
  const todas = histEstado.compras.todas;

  const filtradas = todas.filter(compra => {
    const fecha = new Date(compra.fecha);
    let coincideTiempo = true;
    if (filtroTiempo === "hoy") coincideTiempo = histEsHoy(fecha, ahora);
    else if (filtroTiempo === "semana") coincideTiempo = histEsEstaSemana(fecha, ahora);
    else if (filtroTiempo === "mes") coincideTiempo = histEsEsteMes(fecha, ahora);
    if (!coincideTiempo) return false;
    return !termino || textoBusquedaCompra(compra, fecha).includes(termino);
  });

  histEstado.compras.filtradas = filtradas;
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

  $("tabla-compras-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} compras`;
}

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

/* ==========================================================================
   MODAL: REGISTRAR NUEVA COMPRA
   ========================================================================== */
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
  /* Si el producto ya está en la lista, suma una unidad en lugar de duplicarlo */
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
          <div class="campo-prefijo"><span>$</span><input type="number" min="0" step="any" data-campo="costoUnitario" value="${linea.costoUnitario}"></div>
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
    const malCosto = !Number.isFinite(l.costoUnitario) || l.costoUnitario < 0 || l.costoUnitario > 99999999.99;
    if (fila) {
      fila.querySelector('[data-campo="cantidad"]').classList.toggle("campo-error", malaCant);
      fila.querySelector('[data-campo="costoUnitario"]').classList.toggle("campo-error", malCosto);
    }
    if (malaCant || malCosto) validas = false;
  });
  if (!validas) { errorCompra("Verifique las cantidades (enteros mayores a 0) y los costos ingresados."); return; }

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
