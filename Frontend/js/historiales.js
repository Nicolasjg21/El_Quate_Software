/* ==========================================================================
   HISTORIALES.JS — Lógica funcional del módulo Historiales
   ========================================================================== */

/* ---------- Ícono de acción (ojo — ver detalle), reutilizado en ambas tablas ---------- */
const HIST_ICONO_OJO = `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
  <path d="M1.5 12S5.6 5 12 5s10.5 7 10.5 7-4.1 7-10.5 7-10.5-7-10.5-7Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="12" cy="12" r="3.1" stroke="currentColor" stroke-width="1.8"/>
</svg>`;

/* ---------- Estado de la vista ---------- */
let histEstado = {
  ordenes: { busqueda: "", filtroTiempo: "todos", filtradas: [] },
  compras: { busqueda: "", filtroTiempo: "todos", filtradas: [] },
  lineasCompra: [],
  contadorLinea: 1
};

/* ==========================================================================
   INICIALIZACIÓN
   ========================================================================== */
document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("fecha-actual").textContent = fechaHoy();

  configurarPestañas();
  configurarOrdenes();
  configurarCompras();
  configurarModalOrden();
  configurarModalCompra();

  renderOrdenes();
  renderCompras();
});

/* ==========================================================================
   UTILIDADES GENERALES
   ========================================================================== */
function abrirModal(id) { document.getElementById(id).classList.add("overlay--visible"); }
function cerrarModal(id) { document.getElementById(id).classList.remove("overlay--visible"); }

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

function mostrarToast(mensaje) {
  const toast = document.getElementById("toast");
  toast.textContent = mensaje;
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), 2400);
}

function etiquetaMetodoPago(metodo) {
  if (metodo === "efectivo") return "Efectivo";
  if (metodo === "tarjeta") return "Tarjeta";
  if (metodo === "billetera") return "Billetera digital";
  return metodo;
}

function badgeMetodoPago(metodo) {
  return `<span class="metodo-pago metodo-pago--${metodo}"><span class="metodo-pago-punto"></span>${etiquetaMetodoPago(metodo)}</span>`;
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
  const tabOrdenes = document.getElementById("tab-ordenes");
  const tabCompras = document.getElementById("tab-compras");
  const panelOrdenes = document.getElementById("panel-ordenes");
  const panelCompras = document.getElementById("panel-compras");

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
  tabCompras.addEventListener("click", () => activar("compras"));
}

/* ==========================================================================
   ÓRDENES PAGADAS
   ========================================================================== */
function configurarOrdenes() {
  document.getElementById("buscar-ordenes").addEventListener("input", (e) => {
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

  document.getElementById("btn-exportar-ordenes").addEventListener("click", () => {
    const filas = histEstado.ordenes.filtradas.map(o => [
      o.id, o.mesa, o.mesero, histFormatoFechaHora(new Date(o.fechaHora)),
      o.total, etiquetaMetodoPago(o.metodoPago), o.estado
    ]);
    if (!filas.length) { mostrarToast("No hay registros para exportar."); return; }
    descargarCSV("ordenes-pagadas.csv",
      ["ID Orden", "Mesa", "Mesero", "Fecha y Hora", "Total Pagado (COP)", "Método de Pago", "Estado"],
      filas);
    mostrarToast("Exportación de órdenes pagadas generada.");
  });

  document.getElementById("tabla-ordenes").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-orden]");
    if (btnVer) { abrirDetalleOrden(btnVer.dataset.verOrden); return; }
    const btnLimpiar = e.target.closest("[data-limpiar='ordenes']");
    if (btnLimpiar) { limpiarFiltrosOrdenes(); }
  });
}

function limpiarFiltrosOrdenes() {
  histEstado.ordenes.busqueda = "";
  histEstado.ordenes.filtroTiempo = "todos";
  document.getElementById("buscar-ordenes").value = "";
  document.querySelectorAll("[data-filtro-ordenes]").forEach(b => b.classList.toggle("filtro-btn--activo", b.dataset.filtroOrdenes === "todos"));
  renderOrdenes();
  mostrarToast("Filtros restablecidos");
}

function textoBusquedaOrden(orden, fecha) {
  const itemsTexto = orden.items.map(i => i.nombre).join(" ");
  return histNormalizar([
    orden.id, orden.mesa, orden.mesero, histFormatoFechaHora(fecha), histFormatoFecha(fecha),
    orden.estado, etiquetaMetodoPago(orden.metodoPago), orden.metodoPago,
    orden.total, orden.subtotal, formatoCOP(orden.total), itemsTexto
  ].join(" "));
}

function renderOrdenes() {
  const ahora = new Date();
  const termino = histNormalizar(histEstado.ordenes.busqueda);
  const filtroTiempo = histEstado.ordenes.filtroTiempo;

  const todas = histObtenerOrdenes();

  const filtradas = todas.filter(orden => {
    const fecha = new Date(orden.fechaHora);

    let coincideTiempo = true;
    if (filtroTiempo === "hoy") coincideTiempo = histEsHoy(fecha, ahora);
    else if (filtroTiempo === "semana") coincideTiempo = histEsEstaSemana(fecha, ahora);
    else if (filtroTiempo === "mes") coincideTiempo = histEsEsteMes(fecha, ahora);

    if (!coincideTiempo) return false;

    if (!termino) return true;
    return textoBusquedaOrden(orden, fecha).includes(termino);
  });

  histEstado.ordenes.filtradas = filtradas;

  const cuerpo = document.getElementById("tabla-ordenes");

  if (!filtradas.length) {
    cuerpo.innerHTML = filaEstadoVacio(7, "ordenes");
  } else {
    cuerpo.innerHTML = filtradas.map(orden => {
      const fecha = new Date(orden.fechaHora);
      return `
        <tr>
          <td>${orden.id}</td>
          <td class="col-centro">${orden.mesa}</td>
          <td>${orden.mesero}</td>
          <td>${histFormatoFechaHora(fecha)}</td>
          <td class="col-derecha">${formatoCOP(orden.total)}</td>
          <td class="col-centro">${badgeMetodoPago(orden.metodoPago)}</td>
          <td class="col-centro">
            <button class="accion-ver" data-ver-orden="${orden.id}" title="Ver detalle de la orden" aria-label="Ver detalle de la orden ${orden.id}">${HIST_ICONO_OJO}</button>
          </td>
        </tr>`;
    }).join("");
  }

  document.getElementById("tabla-ordenes-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} órdenes`;
}

/* ---------- Modal: Detalle de la Orden ---------- */
function configurarModalOrden() {
  // el cierre ya está manejado por [data-cerrar]
}

function abrirDetalleOrden(id) {
  const orden = histObtenerOrdenes().find(o => o.id === id);
  if (!orden) { mostrarToast("No fue posible cargar la orden seleccionada."); return; }

  const fecha = new Date(orden.fechaHora);

  document.getElementById("detalle-orden-id").textContent = orden.id;
  document.getElementById("detalle-orden-fecha").textContent = histFormatoFechaHora(fecha);
  document.getElementById("detalle-orden-mesa").textContent = orden.mesa;
  document.getElementById("detalle-orden-mesero").textContent = orden.mesero;
  document.getElementById("detalle-orden-estado").innerHTML = `<span class="pill pill--verde">${orden.estado}</span>`;
  document.getElementById("detalle-orden-metodo").innerHTML = badgeMetodoPago(orden.metodoPago);

  document.getElementById("detalle-orden-items").innerHTML = orden.items.map(item => `
    <tr>
      <td>${item.nombre}</td>
      <td class="col-centro">${item.cantidad}</td>
      <td class="col-derecha">${formatoCOP(item.precioUnitario)}</td>
      <td class="col-derecha">${formatoCOP(item.cantidad * item.precioUnitario)}</td>
    </tr>`).join("");

  document.getElementById("detalle-orden-subtotal").textContent = formatoCOP(orden.subtotal);
  document.getElementById("detalle-orden-impuesto").textContent = formatoCOP(orden.impuesto);
  document.getElementById("detalle-orden-propina").textContent = formatoCOP(orden.propina);
  document.getElementById("detalle-orden-total").textContent = formatoCOP(orden.total);

  abrirModal("overlay-detalle-orden");
}

/* ==========================================================================
   COMPRAS REALIZADAS
   ========================================================================== */
function configurarCompras() {
  document.getElementById("buscar-compras").addEventListener("input", (e) => {
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

  document.getElementById("btn-exportar-compras").addEventListener("click", () => {
    const filas = histEstado.compras.filtradas.map(c => [
      c.id, histFormatoFecha(new Date(c.fecha + "T00:00:00")), c.proveedor,
      c.items.reduce((acc, it) => acc + it.cantidad, 0), c.costoTotal, c.registradoPor
    ]);
    if (!filas.length) { mostrarToast("No hay registros para exportar."); return; }
    descargarCSV("compras-realizadas.csv",
      ["ID Compra", "Fecha", "Proveedor", "Total de Artículos", "Costo Total (COP)", "Registrado por"],
      filas);
    mostrarToast("Exportación de compras generada.");
  });

  document.getElementById("tabla-compras").addEventListener("click", (e) => {
    const btnVer = e.target.closest("[data-ver-compra]");
    if (btnVer) { abrirDetalleCompra(btnVer.dataset.verCompra); return; }
    const btnLimpiar = e.target.closest("[data-limpiar='compras']");
    if (btnLimpiar) { limpiarFiltrosCompras(); }
  });

  document.getElementById("btn-nueva-compra").addEventListener("click", abrirModalNuevaCompra);
}

function limpiarFiltrosCompras() {
  histEstado.compras.busqueda = "";
  histEstado.compras.filtroTiempo = "todos";
  document.getElementById("buscar-compras").value = "";
  document.querySelectorAll("[data-filtro-compras]").forEach(b => b.classList.toggle("filtro-btn--activo", b.dataset.filtroCompras === "todos"));
  renderCompras();
  mostrarToast("Filtros restablecidos");
}

function textoBusquedaCompra(compra, fecha) {
  const itemsTexto = compra.items.map(i => `${i.nombre} ${i.categoria}`).join(" ");
  const totalArticulos = compra.items.reduce((acc, it) => acc + it.cantidad, 0);
  return histNormalizar([
    compra.id, compra.fecha, histFormatoFecha(fecha), compra.proveedor, compra.factura,
    compra.registradoPor, compra.costoTotal, formatoCOP(compra.costoTotal), totalArticulos,
    compra.observaciones || "", itemsTexto
  ].join(" "));
}

function renderCompras() {
  const ahora = new Date();
  const termino = histNormalizar(histEstado.compras.busqueda);
  const filtroTiempo = histEstado.compras.filtroTiempo;

  const todas = histObtenerCompras();

  const filtradas = todas.filter(compra => {
    const fecha = new Date(compra.fecha + "T00:00:00");

    let coincideTiempo = true;
    if (filtroTiempo === "hoy") coincideTiempo = histEsHoy(fecha, ahora);
    else if (filtroTiempo === "semana") coincideTiempo = histEsEstaSemana(fecha, ahora);
    else if (filtroTiempo === "mes") coincideTiempo = histEsEsteMes(fecha, ahora);

    if (!coincideTiempo) return false;

    if (!termino) return true;
    return textoBusquedaCompra(compra, fecha).includes(termino);
  });

  histEstado.compras.filtradas = filtradas;

  const cuerpo = document.getElementById("tabla-compras");

  if (!filtradas.length) {
    cuerpo.innerHTML = filaEstadoVacio(7, "compras");
  } else {
    cuerpo.innerHTML = filtradas.map(compra => {
      const fecha = new Date(compra.fecha + "T00:00:00");
      const totalArticulos = compra.items.reduce((acc, it) => acc + it.cantidad, 0);
      return `
        <tr>
          <td>${compra.id}</td>
          <td>${histFormatoFecha(fecha)}</td>
          <td>${compra.proveedor}</td>
          <td class="col-centro">${totalArticulos}</td>
          <td class="col-derecha">${formatoCOP(compra.costoTotal)}</td>
          <td>${compra.registradoPor}</td>
          <td class="col-centro">
            <button class="accion-ver" data-ver-compra="${compra.id}" title="Ver detalle de la compra" aria-label="Ver detalle de la compra ${compra.id}">${HIST_ICONO_OJO}</button>
          </td>
        </tr>`;
    }).join("");
  }

  document.getElementById("tabla-compras-pie").textContent = `Mostrando ${filtradas.length} de ${todas.length} compras`;
}

/* ---------- Modal: Detalle de la Compra ---------- */
function abrirDetalleCompra(id) {
  const compra = histObtenerCompras().find(c => c.id === id);
  if (!compra) { mostrarToast("No fue posible cargar la compra seleccionada."); return; }

  const fecha = new Date(compra.fecha + "T00:00:00");
  const totalUnidades = compra.items.reduce((acc, it) => acc + it.cantidad, 0);

  document.getElementById("detalle-compra-id").textContent = compra.id;
  document.getElementById("detalle-compra-fecha").textContent = histFormatoFecha(fecha);
  document.getElementById("detalle-compra-proveedor").textContent = compra.proveedor;
  document.getElementById("detalle-compra-usuario").textContent = compra.registradoPor;
  document.getElementById("detalle-compra-factura").textContent = compra.factura || "—";
  document.getElementById("detalle-compra-total-productos").textContent = compra.items.length;
  document.getElementById("detalle-compra-costo-total").textContent = formatoCOP(compra.costoTotal);

  document.getElementById("detalle-compra-items").innerHTML = compra.items.map(item => `
    <tr>
      <td>${item.nombre}</td>
      <td>${item.categoria}</td>
      <td class="col-centro">${item.cantidad}</td>
      <td class="col-derecha">${formatoCOP(item.costoUnitario)}</td>
      <td class="col-derecha">${formatoCOP(item.cantidad * item.costoUnitario)}</td>
    </tr>`).join("");

  document.getElementById("detalle-compra-productos-diferentes").textContent = compra.items.length;
  document.getElementById("detalle-compra-total-unidades").textContent = totalUnidades;
  document.getElementById("detalle-compra-total").textContent = formatoCOP(compra.costoTotal);
  document.getElementById("detalle-compra-observaciones").textContent = compra.observaciones && compra.observaciones.trim() ? compra.observaciones : "Sin observaciones registradas.";

  abrirModal("overlay-detalle-compra");
}

/* ==========================================================================
   MODAL: REGISTRAR NUEVA COMPRA
   ========================================================================== */
function opcionesCategoriasCompra() {
  return [...new Set(HIST_CATALOGO_COMPRAS.map(p => p.categoria))];
}

function poblarSelectProveedores() {
  const select = document.getElementById("input-proveedor-compra");
  select.innerHTML = `<option value="">Seleccionar proveedor</option>` +
    HIST_PROVEEDORES.map(p => `<option value="${p}">${p}</option>`).join("");
}

function opcionesProductoSelect(seleccionado) {
  const categorias = opcionesCategoriasCompra();
  let html = `<option value="">Seleccionar producto</option>`;
  categorias.forEach(cat => {
    html += `<optgroup label="${cat}">`;
    HIST_CATALOGO_COMPRAS.filter(p => p.categoria === cat).forEach(p => {
      const sel = p.nombre === seleccionado ? "selected" : "";
      html += `<option value="${p.nombre}" ${sel}>${p.nombre}</option>`;
    });
    html += `</optgroup>`;
  });
  return html;
}

function configurarModalCompra() {
  poblarSelectProveedores();

  document.getElementById("btn-nueva-compra")?.addEventListener("click", () => {});

  document.getElementById("buscar-producto-compra").addEventListener("input", (e) => {
    renderSugerenciasProducto(e.target.value);
  });

  document.getElementById("buscar-producto-compra").addEventListener("focus", (e) => {
    if (e.target.value.trim()) renderSugerenciasProducto(e.target.value);
  });

  document.addEventListener("click", (e) => {
    if (!e.target.closest(".buscador-producto")) ocultarSugerencias();
  });

  document.getElementById("btn-nuevo-producto-compra").addEventListener("click", () => agregarLineaCompra());
  document.getElementById("btn-agregar-otro-producto").addEventListener("click", () => agregarLineaCompra());

  document.getElementById("tabla-productos-compra").addEventListener("input", manejarCambioLinea);
  document.getElementById("tabla-productos-compra").addEventListener("change", manejarCambioLinea);
  document.getElementById("tabla-productos-compra").addEventListener("click", (e) => {
    const btnQuitar = e.target.closest("[data-quitar-linea]");
    if (btnQuitar) quitarLineaCompra(btnQuitar.dataset.quitarLinea);
  });

  document.getElementById("btn-guardar-compra").addEventListener("click", guardarCompra);
}

function abrirModalNuevaCompra() {
  histEstado.lineasCompra = [];
  histEstado.contadorLinea = 1;

  document.getElementById("input-proveedor-compra").value = "";
  document.getElementById("input-fecha-compra").value = histFechaISO(new Date());
  document.getElementById("input-factura-compra").value = "";
  document.getElementById("input-observaciones-compra").value = "";
  document.getElementById("buscar-producto-compra").value = "";
  ocultarSugerencias();

  document.querySelectorAll("#overlay-nueva-compra .campo-error").forEach(el => el.classList.remove("campo-error"));
  document.querySelectorAll("#overlay-nueva-compra .campo-mensaje-error").forEach(el => el.classList.remove("campo-mensaje-error--visible"));

  renderTablaCompra();
  abrirModal("overlay-nueva-compra");
}

function renderSugerenciasProducto(query) {
  const contenedor = document.getElementById("sugerencias-productos");
  const termino = histNormalizar(query);

  if (!termino) { ocultarSugerencias(); return; }

  const coincidencias = HIST_CATALOGO_COMPRAS.filter(p => histNormalizar(p.nombre).includes(termino)).slice(0, 8);

  if (!coincidencias.length) {
    contenedor.innerHTML = `<div class="sugerencias-vacio">No se encontraron productos.</div>`;
  } else {
    contenedor.innerHTML = coincidencias.map(p => `
      <div class="sugerencia-item" data-sugerencia="${p.nombre}">
        <span>${p.nombre}</span>
        <span class="sugerencia-item-categoria">${p.categoria}</span>
      </div>`).join("");

    contenedor.querySelectorAll("[data-sugerencia]").forEach(item => {
      item.addEventListener("click", () => {
        const producto = HIST_CATALOGO_COMPRAS.find(p => p.nombre === item.dataset.sugerencia);
        agregarLineaCompra(producto);
        document.getElementById("buscar-producto-compra").value = "";
        ocultarSugerencias();
      });
    });
  }

  contenedor.classList.add("sugerencias-productos--visible");
}

function ocultarSugerencias() {
  const contenedor = document.getElementById("sugerencias-productos");
  if (contenedor) contenedor.classList.remove("sugerencias-productos--visible");
}

function agregarLineaCompra(producto) {
  const id = `linea-${histEstado.contadorLinea++}`;
  histEstado.lineasCompra.push({
    id,
    nombre: producto ? producto.nombre : "",
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
  if (!linea) return;

  const campo = e.target.dataset.campo;
  if (!campo) return;

  if (campo === "nombre") {
    linea.nombre = e.target.value;
    const producto = HIST_CATALOGO_COMPRAS.find(p => p.nombre === linea.nombre);
    if (producto) {
      linea.categoria = producto.categoria;
      linea.costoUnitario = producto.costo;
    } else {
      linea.categoria = "";
    }
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
  if (!fila) return;
  const celda = fila.querySelector(".col-subtotal");
  if (celda) celda.textContent = formatoCOP(Math.max(0, linea.cantidad) * Math.max(0, linea.costoUnitario));
}

function renderTablaCompra() {
  const cuerpo = document.getElementById("tabla-productos-compra");
  const scrollTabla = cuerpo.closest(".tabla-scroll");
  const estadoVacio = document.getElementById("estado-vacio-compra");

  if (!histEstado.lineasCompra.length) {
    cuerpo.innerHTML = "";
    scrollTabla.style.display = "none";
    estadoVacio.style.display = "flex";
  } else {
    scrollTabla.style.display = "";
    estadoVacio.style.display = "none";

    cuerpo.innerHTML = histEstado.lineasCompra.map(linea => `
      <tr data-linea-id="${linea.id}">
        <td><select data-campo="nombre">${opcionesProductoSelect(linea.nombre)}</select></td>
        <td><input type="text" data-campo="categoria" value="${linea.categoria}" readonly placeholder="—"></td>
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
  const lineasValidas = histEstado.lineasCompra.filter(l => l.nombre);
  const totalUnidades = lineasValidas.reduce((acc, l) => acc + Math.max(0, l.cantidad), 0);
  const costoTotal = lineasValidas.reduce((acc, l) => acc + Math.max(0, l.cantidad) * Math.max(0, l.costoUnitario), 0);

  document.getElementById("resumen-productos-diferentes").textContent = lineasValidas.length;
  document.getElementById("resumen-total-unidades").textContent = totalUnidades;
  document.getElementById("resumen-costo-total").textContent = formatoCOP(costoTotal);
}

function marcarErrorElemento(elemento, mostrar) {
  if (!elemento) return;
  elemento.classList.toggle("campo-error", mostrar);
}

function guardarCompra() {
  document.querySelectorAll("#overlay-nueva-compra .campo-error").forEach(el => el.classList.remove("campo-error"));

  const proveedor = document.getElementById("input-proveedor-compra").value;
  const fecha = document.getElementById("input-fecha-compra").value;
  const factura = document.getElementById("input-factura-compra").value.trim();
  const observaciones = document.getElementById("input-observaciones-compra").value.trim();

  if (!proveedor) {
    marcarErrorElemento(document.getElementById("input-proveedor-compra"), true);
    mostrarToast("Seleccione un proveedor.");
    return;
  }

  if (!fecha) {
    marcarErrorElemento(document.getElementById("input-fecha-compra"), true);
    mostrarToast("Ingrese una fecha de compra válida.");
    return;
  }

  const lineasValidas = histEstado.lineasCompra.filter(l => l.nombre);

  if (!lineasValidas.length) {
    mostrarToast("Agregue al menos un producto a la compra.");
    return;
  }

  let cantidadesValidas = true;
  lineasValidas.forEach(l => {
    const fila = document.querySelector(`tr[data-linea-id="${l.id}"]`);
    const inputCantidad = fila ? fila.querySelector('[data-campo="cantidad"]') : null;
    const inputCosto = fila ? fila.querySelector('[data-campo="costoUnitario"]') : null;
    const cantidadInvalida = !Number.isInteger(l.cantidad) || l.cantidad <= 0;
    const costoInvalido = isNaN(l.costoUnitario) || l.costoUnitario < 0;
    marcarErrorElemento(inputCantidad, cantidadInvalida);
    marcarErrorElemento(inputCosto, costoInvalido);
    if (cantidadInvalida || costoInvalido) cantidadesValidas = false;
  });

  if (!cantidadesValidas) {
    mostrarToast("Verifique las cantidades y costos ingresados.");
    return;
  }

  const items = lineasValidas.map(l => ({
    nombre: l.nombre,
    categoria: l.categoria || "Sin categoría",
    cantidad: Number(l.cantidad),
    costoUnitario: Number(l.costoUnitario)
  }));

  const costoTotal = items.reduce((acc, it) => acc + it.cantidad * it.costoUnitario, 0);

  const nuevaCompra = {
    id: histSiguienteIdCompra(),
    fecha,
    proveedor,
    factura: factura || "",
    registradoPor: "Usuario Administrador",
    items,
    costoTotal,
    observaciones
  };

  const compras = histObtenerCompras();
  compras.unshift(nuevaCompra);
  histGuardarCompras(compras);

  cerrarModal("overlay-nueva-compra");
  renderCompras();
  mostrarToast("Compra registrada correctamente.");
}
