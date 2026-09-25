/* ==========================================================================
   MESAS.JS — Lógica funcional del módulo Mesas
   ========================================================================== */

let filtroActual = "todas";
let mesaSeleccionadaId = null;

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("fecha-actual").textContent = fechaHoy();

  renderMesas();

  document.querySelectorAll(".filtro-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".filtro-btn").forEach(b => b.classList.remove("filtro-btn--activo"));
      btn.classList.add("filtro-btn--activo");
      filtroActual = btn.dataset.filtro;
      renderMesas();
    });
  });

  document.getElementById("btn-nueva-mesa").addEventListener("click", () => abrirModal("overlay-nueva-mesa"));
  document.getElementById("btn-registrar-mesa").addEventListener("click", registrarNuevaMesa);

  document.querySelectorAll("[data-cerrar]").forEach(btn => {
    btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
  });

  document.getElementById("btn-volver-mesas").addEventListener("click", volverAMesas);
  document.getElementById("btn-agregar-productos").addEventListener("click", () => {
    renderPickerProductos("");
    abrirModal("overlay-agregar-producto");
  });
  document.getElementById("buscar-producto-picker").addEventListener("input", (e) => {
    renderPickerProductos(e.target.value);
  });

  document.getElementById("btn-confirmar-pago").addEventListener("click", confirmarPago);
  document.getElementById("btn-marcar-pagada").addEventListener("click", marcarComoPagada);

  document.getElementById("input-numero-mesa").addEventListener("keydown", (e) => {
    if (e.key === "Enter") registrarNuevaMesa();
  });
});

/* ---------- Modales genéricos ---------- */
function abrirModal(id) {
  document.getElementById(id).classList.add("overlay--visible");
}
function cerrarModal(id) {
  document.getElementById(id).classList.remove("overlay--visible");
  document.getElementById("error-numero-mesa").classList.remove("campo-mensaje-error--visible");
  document.getElementById("input-numero-mesa").classList.remove("campo-error");
}

function mostrarToast(mensaje) {
  const toast = document.getElementById("toast");
  toast.textContent = mensaje;
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), 2200);
}

/* ---------- Vista: listado de mesas ---------- */
function renderMesas() {
  const mesas = obtenerMesas().sort((a, b) => a.numero - b.numero);
  const total = mesas.length;

  const conteo = { libre: 0, ocupada: 0, cerrando: 0 };
  mesas.forEach(m => conteo[calcularEstadoMesa(m)]++);

  document.getElementById("conteo-ocupadas").innerHTML = `${conteo.ocupada} <small>/ ${total}</small>`;
  document.getElementById("conteo-libres").innerHTML = `${conteo.libre} <small>/ ${total}</small>`;
  document.getElementById("conteo-cerrando").innerHTML = `${conteo.cerrando} <small>/ ${total}</small>`;

  const visibles = mesas.filter(m => filtroActual === "todas" || calcularEstadoMesa(m) === filtroActual);

  const grid = document.getElementById("mesas-grid");
  grid.innerHTML = "";

  if (visibles.length === 0) {
    grid.innerHTML = `<p class="picker-vacio">No hay mesas con este estado.</p>`;
    return;
  }

  visibles.forEach(mesa => grid.appendChild(mesaCardEl(mesa)));
}

function mesaCardEl(mesa) {
  const estado = calcularEstadoMesa(mesa);
  const { total } = calcularTotalesMesa(mesa);

  const card = document.createElement("button");
  card.type = "button";
  card.className = `mesa-card mesa-card--${estado}`;
  card.addEventListener("click", () => abrirDetalle(mesa.id));

  let cuerpo = "";
  if (estado === "libre") {
    cuerpo = `
      <div class="mesa-card-disponible">Disponible</div>
      <div class="mesa-card-estado-libre"><span class="pill pill--verde">Libre</span></div>
    `;
  } else {
    const etiquetaCaption = estado === "cerrando" ? "Cerrando cuenta" : "Total actual";
    cuerpo = `
      <p class="mesa-card-info">&#128337; ${mesa.abiertoDesde || "--:--"}</p>
      <p class="mesa-card-info">&#128101; ${mesa.personas} persona${mesa.personas === 1 ? "" : "s"}</p>
      <div class="mesa-card-total">
        <p class="mesa-card-total-valor">${formatoCOP(total)}</p>
        <p class="mesa-card-total-etiqueta">${etiquetaCaption}</p>
      </div>
    `;
  }

  card.innerHTML = `
    <div class="mesa-card-fila-superior">
      <span class="mesa-card-nombre">Mesa ${mesa.numero}</span>
      <span class="mesa-card-punto mesa-card-punto--${estado}"></span>
    </div>
    ${cuerpo}
  `;
  return card;
}

/* ---------- Nueva mesa ---------- */
function registrarNuevaMesa() {
  const input = document.getElementById("input-numero-mesa");
  const error = document.getElementById("error-numero-mesa");
  const numero = parseInt(input.value, 10);

  const mesas = obtenerMesas();
  const invalido = !numero || numero < 1 || mesas.some(m => m.numero === numero);

  if (invalido) {
    input.classList.add("campo-error");
    error.classList.add("campo-mensaje-error--visible");
    return;
  }

  const nuevoId = mesas.length ? Math.max(...mesas.map(m => m.id)) + 1 : 1;
  mesas.push({ id: nuevoId, numero, personas: 0, abiertoDesde: null, items: [], estadoPago: "ninguno" });
  guardarMesas(mesas);

  input.value = "";
  cerrarModal("overlay-nueva-mesa");
  renderMesas();
  mostrarToast(`Mesa ${numero} registrada`);
}

/* ---------- Vista: detalle de mesa ---------- */
function abrirDetalle(id) {
  mesaSeleccionadaId = id;
  document.getElementById("vista-mesas").style.display = "none";
  document.getElementById("vista-detalle").style.display = "block";
  renderDetalle();
}

function volverAMesas() {
  mesaSeleccionadaId = null;
  document.getElementById("vista-detalle").style.display = "none";
  document.getElementById("vista-mesas").style.display = "block";
  renderMesas();
}

function renderDetalle() {
  const mesa = obtenerMesaPorId(mesaSeleccionadaId);
  if (!mesa) { volverAMesas(); return; }

  document.getElementById("detalle-titulo").textContent = `Mesa ${mesa.numero}`;
  document.getElementById("detalle-meta").innerHTML =
    `&#128101; ${mesa.personas} persona${mesa.personas === 1 ? "" : "s"} &nbsp;&nbsp; &#128337; ${mesa.abiertoDesde ? "Abierto desde " + mesa.abiertoDesde : "Aún no se ha abierto"}`;

  const { filas, subtotal, iva, total } = calcularTotalesMesa(mesa);

  const cuerpo = document.getElementById("detalle-items");
  const vacio = document.getElementById("detalle-vacio");
  cuerpo.innerHTML = "";

  if (filas.length === 0) {
    vacio.style.display = "block";
  } else {
    vacio.style.display = "none";
    filas.forEach(fila => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${fila.nombre}</td>
        <td class="col-centro">
          <div class="cant-control">
            <button data-accion="restar" data-id="${fila.productoId}">−</button>
            <span>${fila.cantidad}</span>
            <button data-accion="sumar" data-id="${fila.productoId}" ${fila.stockDisponible <= 0 ? "disabled" : ""}>+</button>
          </div>
        </td>
        <td class="col-derecha">${formatoCOP(fila.precio)}</td>
        <td class="col-derecha">${formatoCOP(fila.subtotal)}</td>
        <td class="col-centro"><button class="btn-quitar" data-accion="quitar" data-id="${fila.productoId}">&#10005;</button></td>
      `;
      cuerpo.appendChild(tr);
    });
  }

  document.getElementById("detalle-subtotal").textContent = formatoCOP(subtotal);
  document.getElementById("detalle-iva").textContent = formatoCOP(iva);
  document.getElementById("detalle-total").textContent = formatoCOP(total);

  const enCerrando = mesa.estadoPago === "cerrando";
  document.getElementById("btn-confirmar-pago").style.display = enCerrando ? "none" : "block";
  document.getElementById("btn-marcar-pagada").style.display = enCerrando ? "block" : "none";
  document.getElementById("btn-agregar-productos").disabled = enCerrando;
  document.getElementById("btn-agregar-productos").style.opacity = enCerrando ? "0.5" : "1";

  cuerpo.querySelectorAll("button[data-accion]").forEach(btn => {
    btn.addEventListener("click", () => manejarAccionItem(btn.dataset.accion, btn.dataset.id));
  });
}

function manejarAccionItem(accion, productoId) {
  const mesas = obtenerMesas();
  const mesa = mesas.find(m => m.id === mesaSeleccionadaId);
  const item = mesa.items.find(i => i.productoId === productoId);
  if (!item) return;

  if (accion === "sumar") {
    if (!ajustarStock(productoId, -1)) return;
    item.cantidad++;
  } else if (accion === "restar") {
    item.cantidad--;
    ajustarStock(productoId, 1);
    if (item.cantidad <= 0) {
      mesa.items = mesa.items.filter(i => i.productoId !== productoId);
    }
  } else if (accion === "quitar") {
    ajustarStock(productoId, item.cantidad);
    mesa.items = mesa.items.filter(i => i.productoId !== productoId);
  }

  guardarMesas(mesas);
  renderDetalle();
}

/* ---------- Agregar productos (picker) ---------- */
function renderPickerProductos(filtroTexto) {
  const productos = obtenerProductos().sort((a, b) => a.nombre.localeCompare(b.nombre));
  const texto = filtroTexto.trim().toLowerCase();
  const filtrados = productos.filter(p =>
    p.nombre.toLowerCase().includes(texto) || p.categoria.toLowerCase().includes(texto)
  );

  const lista = document.getElementById("lista-productos-picker");
  lista.innerHTML = "";

  if (filtrados.length === 0) {
    lista.innerHTML = `<p class="picker-vacio">No se encontraron productos.</p>`;
    return;
  }

  filtrados.forEach(p => {
    const item = document.createElement("div");
    item.className = "picker-item";
    const sinStock = p.stock <= 0;
    item.innerHTML = `
      <div class="picker-item-info">
        <p class="picker-item-nombre">${p.nombre}</p>
        <p class="picker-item-meta ${sinStock ? "picker-item-meta--bajo" : ""}">${p.categoria} · ${formatoCOP(p.precio)} · Stock: ${p.stock}</p>
      </div>
      <div class="picker-item-acciones">
        <button class="btn btn--primario" data-agregar="${p.id}" ${sinStock ? "disabled" : ""}>${sinStock ? "Sin stock" : "+ Agregar"}</button>
      </div>
    `;
    lista.appendChild(item);
  });

  lista.querySelectorAll("[data-agregar]").forEach(btn => {
    btn.addEventListener("click", () => agregarProductoAMesa(btn.dataset.agregar));
  });
}

function agregarProductoAMesa(productoId) {
  const mesas = obtenerMesas();
  const mesa = mesas.find(m => m.id === mesaSeleccionadaId);

  if (!ajustarStock(productoId, -1)) return;

  if (!mesa.abiertoDesde) mesa.abiertoDesde = horaActual();
  if (!mesa.personas || mesa.personas < 1) mesa.personas = 1;

  const existente = mesa.items.find(i => i.productoId === productoId);
  if (existente) existente.cantidad++;
  else mesa.items.push({ productoId, cantidad: 1 });

  guardarMesas(mesas);
  renderDetalle();
  renderPickerProductos(document.getElementById("buscar-producto-picker").value);
}

/* ---------- Confirmar pago / liberar mesa ---------- */
function confirmarPago() {
  const mesas = obtenerMesas();
  const mesa = mesas.find(m => m.id === mesaSeleccionadaId);
  if (!mesa.items.length) return;
  mesa.estadoPago = "cerrando";
  guardarMesas(mesas);
  renderDetalle();
  mostrarToast(`Mesa ${mesa.numero} pasó a "Cerrando" mientras se procesa el pago`);
}

function marcarComoPagada() {
  const mesas = obtenerMesas();
  const mesa = mesas.find(m => m.id === mesaSeleccionadaId);
  mesa.items = [];
  mesa.estadoPago = "ninguno";
  mesa.personas = 0;
  mesa.abiertoDesde = null;
  guardarMesas(mesas);
  mostrarToast(`Mesa ${mesa.numero} liberada`);
  volverAMesas();
}
