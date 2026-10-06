/* ==========================================================================
   MESAS.JS — Módulo Mesas (conectado a la API vía Negocio, ver api-negocio.js)
   --------------------------------------------------------------------------
   Flujo: Libre -> (agregar productos) Ocupada -> (Confirmar pago) Cerrando
          -> (Caja elige método de pago y cobra) Libre
   - Mesero y Caja agregan / quitan productos y confirman el pago.
   - Solo quien tiene el permiso cobros.gestionar (Caja, Administración) cobra.
   - Solo Administración crea o elimina mesas.
   ========================================================================== */

let filtroActual = "todas";
let mesas = [];            // última consulta (Mesas + cuenta abierta de cada una)
let catalogo = [];         // productos con stock (para nombres y selector)
let mesaSeleccionada = null;
let detalle = null;        // { cuenta, pedidos, items, comprobantes } de la mesa abierta
let ocupado = false;       // evita dobles clics mientras una operación está en curso

const $ = (id) => document.getElementById(id);

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  document.querySelectorAll(".filtro-btn").forEach(btn => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".filtro-btn").forEach(b => b.classList.remove("filtro-btn--activo"));
      btn.classList.add("filtro-btn--activo");
      filtroActual = btn.dataset.filtro;
      renderMesas();
    });
  });

  $("btn-nueva-mesa").addEventListener("click", () => abrirModal("overlay-nueva-mesa"));
  $("btn-registrar-mesa").addEventListener("click", registrarNuevaMesa);
  $("input-numero-mesa").addEventListener("keydown", (e) => { if (e.key === "Enter") registrarNuevaMesa(); });

  document.querySelectorAll("[data-cerrar]").forEach(btn => {
    btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
  });

  $("btn-volver-mesas").addEventListener("click", volverAMesas);
  $("btn-agregar-productos").addEventListener("click", abrirPicker);
  $("buscar-producto-picker").addEventListener("input", (e) => renderPickerProductos(e.target.value));

  $("btn-confirmar-pago").addEventListener("click", confirmarPago);
  $("btn-marcar-pagada").addEventListener("click", abrirCobro);
  $("btn-reabrir-cuenta").addEventListener("click", reabrirCuenta);
  $("btn-confirmar-cobro").addEventListener("click", confirmarCobro);
  $("btn-agregar-metodo").addEventListener("click", agregarMetodoPago);
  $("btn-eliminar-mesa").addEventListener("click", () => abrirModal("overlay-eliminar-mesa"));
  $("btn-confirmar-eliminar-mesa").addEventListener("click", eliminarMesa);

  /* Delegación de los botones +, −, quitar de cada línea */
  $("detalle-items").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (btn) manejarAccionItem(btn.dataset.accion, Number(btn.dataset.id));
  });

  Auth.listo.then(() => {
    /* Mesero y Caja no crean ni eliminan mesas: solo Administración */
    $("btn-nueva-mesa").hidden = !Auth.esAdmin();
    cargarMesas();
  });
});

/* ---------- Modales y avisos ---------- */
function abrirModal(id) { $(id).classList.add("overlay--visible"); }
function cerrarModal(id) {
  $(id).classList.remove("overlay--visible");
  if (id === "overlay-nueva-mesa") {
    $("error-numero-mesa").classList.remove("campo-mensaje-error--visible");
    $("input-numero-mesa").classList.remove("campo-error");
  }
}

function mostrarToast(mensaje, tipo) {
  const toast = $("toast");
  toast.textContent = mensaje;
  toast.classList.toggle("toast--error", tipo === "error");
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), tipo === "error" ? 4200 : 2400);
}

/** Ejecuta una operación asíncrona evitando solapes y mostrando el error del Backend (400/403/404/409/500…). */
async function ejecutar(operacion, textoError) {
  if (ocupado) return false;
  ocupado = true;
  try {
    await operacion();
    return true;
  } catch (err) {
    mostrarToast(Api.mensajeError(err, textoError || "No fue posible completar la operación."), "error");
    return false;
  } finally {
    ocupado = false;
  }
}

const horaDe = (iso) => {
  const d = iso ? new Date(iso) : null;
  return d && !isNaN(d) ? d.getHours().toString().padStart(2, "0") + ":" + d.getMinutes().toString().padStart(2, "0") : "--:--";
};

/* ---------- Vista: listado de mesas ---------- */
async function cargarMesas() {
  $("mesas-grid").innerHTML = `<p class="picker-vacio">Cargando mesas...</p>`;
  try {
    mesas = await Negocio.cargarMesas();
    renderMesas();
  } catch (err) {
    $("mesas-grid").innerHTML = `<p class="picker-vacio">${esc(Api.mensajeError(err, "No fue posible cargar las mesas."))}</p>`;
  }
}

function renderMesas() {
  const total = mesas.length;
  const conteo = { libre: 0, ocupada: 0, cerrando: 0 };
  mesas.forEach(m => conteo[m.estado]++);

  $("conteo-ocupadas").innerHTML = `${conteo.ocupada} <small>/ ${total}</small>`;
  $("conteo-libres").innerHTML = `${conteo.libre} <small>/ ${total}</small>`;
  $("conteo-cerrando").innerHTML = `${conteo.cerrando} <small>/ ${total}</small>`;

  const visibles = mesas.filter(m => filtroActual === "todas" || m.estado === filtroActual);
  const grid = $("mesas-grid");
  grid.innerHTML = "";

  if (total === 0) {
    grid.innerHTML = `<p class="picker-vacio">No hay mesas registradas.</p>`;
    return;
  }
  if (visibles.length === 0) {
    grid.innerHTML = `<p class="picker-vacio">No hay mesas con este estado.</p>`;
    return;
  }
  visibles.forEach(mesa => grid.appendChild(mesaCardEl(mesa)));
}

function mesaCardEl(mesa) {
  const estado = mesa.estado;
  const card = document.createElement("button");
  card.type = "button";
  card.className = `mesa-card mesa-card--${estado}`;
  card.addEventListener("click", () => abrirDetalle(mesa.idMesa));

  let cuerpo;
  if (estado === "libre") {
    cuerpo = `
      <div class="mesa-card-disponible">Disponible</div>
      <div class="mesa-card-estado-libre"><span class="pill pill--verde">Libre</span></div>`;
  } else {
    const etiqueta = estado === "cerrando" ? "Cerrando cuenta" : "Total actual";
    cuerpo = `
      <p class="mesa-card-info">&#128337; ${horaDe(mesa.cuenta && mesa.cuenta.fechaApertura)}</p>
      <div class="mesa-card-total">
        <p class="mesa-card-total-valor">${formatoCOP(mesa.cuenta ? mesa.cuenta.total : 0)}</p>
        <p class="mesa-card-total-etiqueta">${etiqueta}</p>
      </div>`;
  }

  card.innerHTML = `
    <div class="mesa-card-fila-superior">
      <span class="mesa-card-nombre">Mesa ${esc(mesa.numero)}</span>
      <span class="mesa-card-punto mesa-card-punto--${estado}"></span>
    </div>
    ${cuerpo}`;
  return card;
}

/* ---------- Nueva mesa (solo Administración) ---------- */
async function registrarNuevaMesa() {
  const input = $("input-numero-mesa");
  const error = $("error-numero-mesa");
  const numero = parseInt(input.value, 10);

  const invalido = !Number.isInteger(numero) || numero < 1 || mesas.some(m => m.numero === numero);
  input.classList.toggle("campo-error", invalido);
  error.classList.toggle("campo-mensaje-error--visible", invalido);
  if (invalido) return;

  const ok = await ejecutar(async () => {
    try {
      await Negocio.crearMesa(numero);
    } catch (err) {
      if (err && (err.status === 404 || err.status === 405)) {
        throw { status: err.status, mensaje: "El servidor aún no tiene habilitada la creación de mesas (endpoint api/Mesas/PostMesa). Pida que se agregue en el Backend." };
      }
      throw err;
    }
    input.value = "";
    cerrarModal("overlay-nueva-mesa");
    mostrarToast(`Mesa ${numero} registrada`);
  }, "No fue posible registrar la mesa.");
  if (ok) await cargarMesas();
}

/* ---------- Vista: detalle de mesa ---------- */
async function abrirDetalle(idMesa) {
  mesaSeleccionada = mesas.find(m => m.idMesa === idMesa) || null;
  if (!mesaSeleccionada) return;
  $("vista-mesas").style.display = "none";
  $("vista-detalle").style.display = "block";
  detalle = { cuenta: mesaSeleccionada.cuenta, pedidos: [], items: [], comprobantes: [] };
  $("detalle-titulo").textContent = `Mesa ${mesaSeleccionada.numero}`;
  $("detalle-meta").textContent = "Cargando cuenta...";
  $("detalle-items").innerHTML = "";

  try {
    [detalle, catalogo] = await Promise.all([Negocio.cargarDetalle(mesaSeleccionada), Negocio.productos()]);
    renderDetalle();
  } catch (err) {
    mostrarToast(Api.mensajeError(err, "No fue posible cargar la cuenta de la mesa."), "error");
    volverAMesas();
  }
}

async function volverAMesas() {
  mesaSeleccionada = null;
  detalle = null;
  $("vista-detalle").style.display = "none";
  $("vista-mesas").style.display = "block";
  await cargarMesas();
}

function nombreProducto(idProducto) {
  const p = catalogo.find(x => x.idProducto === idProducto);
  return p ? p.nombre : `Producto #${idProducto}`;
}
function stockProducto(idProducto) {
  const p = catalogo.find(x => x.idProducto === idProducto);
  return p ? p.stock : 0;
}

function renderDetalle() {
  const mesa = mesaSeleccionada;
  if (!mesa || !detalle) return;

  $("detalle-titulo").textContent = `Mesa ${mesa.numero}`;
  $("detalle-meta").innerHTML = `&#128337; ${mesa.cuenta ? "Abierto desde " + horaDe(mesa.cuenta.fechaApertura) : "Aún no se ha abierto"}`;

  const t = Negocio.totales(detalle.items);
  const cuerpo = $("detalle-items");
  const vacio = $("detalle-vacio");
  const cerrando = mesa.estado === "cerrando";
  cuerpo.innerHTML = "";

  if (detalle.items.length === 0) {
    vacio.style.display = "block";
  } else {
    vacio.style.display = "none";
    cuerpo.innerHTML = detalle.items.map(i => `
      <tr>
        <td>${esc(nombreProducto(i.idProducto))}</td>
        <td class="col-centro">
          <div class="cant-control">
            <button data-accion="restar" data-id="${i.idDetalle}" ${cerrando ? "disabled" : ""}>−</button>
            <span>${i.cantidad}</span>
            <button data-accion="sumar" data-id="${i.idDetalle}" ${cerrando || stockProducto(i.idProducto) <= 0 ? "disabled" : ""}>+</button>
          </div>
        </td>
        <td class="col-derecha">${formatoCOP(i.precioUnitario)}</td>
        <td class="col-derecha">${formatoCOP(i.cantidad * i.precioUnitario)}</td>
        <td class="col-centro"><button class="btn-quitar" data-accion="quitar" data-id="${i.idDetalle}" ${cerrando ? "disabled" : ""}>&#10005;</button></td>
      </tr>`).join("");
  }

  $("detalle-subtotal").textContent = formatoCOP(t.subtotal);
  $("detalle-iva").textContent = formatoCOP(t.iva);
  $("detalle-total").textContent = formatoCOP(t.total);

  const puedeCobrar = Auth.tiene("cobros.gestionar");
  $("btn-confirmar-pago").style.display = cerrando ? "none" : "block";
  $("btn-confirmar-pago").disabled = detalle.items.length === 0;
  $("btn-marcar-pagada").style.display = cerrando && puedeCobrar ? "block" : "none";
  $("btn-reabrir-cuenta").style.display = cerrando ? "block" : "none";
  $("nota-cobro-caja").style.display = cerrando && !puedeCobrar ? "block" : "none";
  $("btn-agregar-productos").disabled = cerrando;
  $("btn-agregar-productos").style.opacity = cerrando ? "0.5" : "1";

  /* Eliminar mesa: solo Administración y solo si no tiene cuenta abierta */
  $("btn-eliminar-mesa").hidden = !(Auth.esAdmin() && mesa.estado === "libre" && !mesa.cuenta);
}

async function manejarAccionItem(accion, idDetalle) {
  const mesa = mesaSeleccionada;
  if (accion === "quitar") {
    const it = detalle && detalle.items.find(i => i.idDetalle === idDetalle);
    const ok = await Confirmar.pedir({
      titulo: "Quitar producto",
      mensaje: "¿Desea quitar <strong>" + esc(it ? nombreProducto(it.idProducto) : "este producto") + "</strong> de la cuenta? El stock se devolverá al inventario.",
      textoConfirmar: "Sí, quitar", peligro: true
    });
    if (!ok) return;
  }
  const ok = await ejecutar(async () => {
    if (accion === "sumar") await Negocio.cambiarCantidad(mesa, detalle, idDetalle, +1);
    else if (accion === "restar") await Negocio.cambiarCantidad(mesa, detalle, idDetalle, -1);
    else if (accion === "quitar") await Negocio.quitarItem(mesa, detalle, idDetalle);
  }, "No fue posible actualizar el pedido.");
  if (ok) {
    catalogo = await Negocio.productos().catch(() => catalogo);
    renderDetalle();
  }
}

/* ---------- Agregar productos (selector) ---------- */
async function abrirPicker() {
  $("buscar-producto-picker").value = "";
  $("lista-productos-picker").innerHTML = `<p class="picker-vacio">Cargando productos...</p>`;
  abrirModal("overlay-agregar-producto");
  try {
    catalogo = await Negocio.productos();
    renderPickerProductos("");
  } catch (err) {
    $("lista-productos-picker").innerHTML = `<p class="picker-vacio">${esc(Api.mensajeError(err, "No fue posible cargar los productos."))}</p>`;
  }
}

function renderPickerProductos(filtroTexto) {
  const texto = Negocio.norm(filtroTexto);
  const filtrados = catalogo
    .filter(p => p.activo)
    .filter(p => Negocio.norm(p.nombre).includes(texto) || Negocio.norm(p.categoria).includes(texto))
    .sort((a, b) => a.nombre.localeCompare(b.nombre));

  const lista = $("lista-productos-picker");
  if (filtrados.length === 0) {
    lista.innerHTML = `<p class="picker-vacio">No se encontraron productos.</p>`;
    return;
  }

  lista.innerHTML = filtrados.map(p => {
    const sinStock = p.stock <= 0;
    return `
      <div class="picker-item">
        <div class="picker-item-info">
          <p class="picker-item-nombre">${esc(p.nombre)}</p>
          <p class="picker-item-meta ${sinStock ? "picker-item-meta--bajo" : ""}">${esc(p.categoria)} · ${formatoCOP(p.precio)} · Stock: ${p.stock}</p>
        </div>
        <div class="picker-item-acciones">
          <button class="btn btn--primario" data-agregar="${p.idProducto}" ${sinStock ? "disabled" : ""}>${sinStock ? "Sin stock" : "+ Agregar"}</button>
        </div>
      </div>`;
  }).join("");

  lista.querySelectorAll("[data-agregar]").forEach(btn => {
    btn.addEventListener("click", () => agregarProductoAMesa(Number(btn.dataset.agregar)));
  });
}

async function agregarProductoAMesa(idProducto) {
  const producto = catalogo.find(p => p.idProducto === idProducto);
  if (!producto) return;
  const ok = await ejecutar(
    () => Negocio.agregarProducto(mesaSeleccionada, detalle, producto),
    "No fue posible agregar el producto."
  );
  if (ok) {
    catalogo = await Negocio.productos().catch(() => catalogo);
    renderDetalle();
    renderPickerProductos($("buscar-producto-picker").value);
  }
}

/* ---------- Confirmar pago (mesa -> Cerrando) ---------- */
async function confirmarPago() {
  const mesa = mesaSeleccionada;
  const ok = await ejecutar(
    () => Negocio.confirmarPago(mesa, detalle),
    "No fue posible confirmar el pago."
  );
  if (ok) {
    renderDetalle();
    mostrarToast(`Mesa ${mesa.numero} pasó a "Cerrando": pendiente de cobro en caja`);
  }
}

async function reabrirCuenta() {
  const mesa = mesaSeleccionada;
  const okReabrir = await Confirmar.pedir({ titulo: "Reabrir cuenta", mensaje: "La mesa <strong>" + esc(mesa.numero) + "</strong> volverá a estado Ocupada. ¿Desea continuar?", textoConfirmar: "Sí, reabrir" });
  if (!okReabrir) return;
  const ok = await ejecutar(() => Negocio.reabrirCuenta(mesa), "No fue posible reabrir la cuenta.");
  if (ok) {
    renderDetalle();
    mostrarToast(`Mesa ${mesa.numero} volvió a "Ocupada"`);
  }
}

/* ---------- Cobro: método de pago y liberación de la mesa ---------- */
let metodos = [];

async function abrirCobro() {
  if (!detalle || !detalle.items.length) return;
  $("cobro-total").textContent = formatoCOP(Negocio.totales(detalle.items).total);
  $("cobro-error").textContent = "";
  $("select-metodo-pago").innerHTML = `<option value="">Cargando métodos...</option>`;
  $("bloque-nuevo-metodo").hidden = true;
  abrirModal("overlay-cobro");
  await cargarMetodos();
}

async function cargarMetodos() {
  try {
    metodos = await Negocio.metodosPago();
  } catch (err) {
    metodos = [];
    $("cobro-error").textContent = Api.mensajeError(err, "No fue posible cargar los métodos de pago.");
  }
  const select = $("select-metodo-pago");
  select.innerHTML = `<option value="">Seleccione un método de pago</option>` +
    metodos.map(m => `<option value="${m.idMetodo}">${esc(m.nombreMetodo)}</option>`).join("");
  if (metodos.length === 1) select.value = String(metodos[0].idMetodo);

  /* Sin métodos registrados: quien puede cobrar puede crear uno aquí mismo */
  $("bloque-nuevo-metodo").hidden = !(metodos.length === 0 && Auth.tiene("cobros.gestionar"));
  if (metodos.length === 0 && !$("cobro-error").textContent) {
    $("cobro-error").textContent = "No hay métodos de pago registrados. Agregue uno para poder cobrar.";
  }
}

async function agregarMetodoPago() {
  const nombre = $("input-nuevo-metodo").value.trim();
  if (!nombre) { $("cobro-error").textContent = "Escriba el nombre del método de pago."; return; }
  if (nombre.length > 50) { $("cobro-error").textContent = "El nombre no puede superar 50 caracteres."; return; }
  const ok = await ejecutar(async () => {
    await Negocio.crearMetodoPago(nombre);
    $("input-nuevo-metodo").value = "";
    $("cobro-error").textContent = "";
  }, "No fue posible crear el método de pago.");
  if (ok) await cargarMetodos();
}

async function confirmarCobro() {
  const idMetodo = Number($("select-metodo-pago").value);
  if (!idMetodo) { $("cobro-error").textContent = "Seleccione un método de pago."; return; }
  $("cobro-error").textContent = "";

  const mesa = mesaSeleccionada;
  const numero = mesa.numero;
  const ok = await ejecutar(
    () => Negocio.cobrar(mesa, detalle, idMetodo),
    "No fue posible registrar el pago."
  );
  if (ok) {
    cerrarModal("overlay-cobro");
    mostrarToast(`Pago registrado. Mesa ${numero} liberada`);
    await volverAMesas();
  }
}

/* ---------- Eliminar mesa (solo Administración, mesa libre) ---------- */
async function eliminarMesa() {
  const mesa = mesaSeleccionada;
  const ok = await ejecutar(async () => {
    try {
      await Negocio.eliminarMesa(mesa);
    } catch (err) {
      if (err && err.status === 409) {
        throw { status: 409, mensaje: "No es posible eliminar la mesa porque tiene cuentas o pedidos asociados." };
      }
      throw err;
    }
    cerrarModal("overlay-eliminar-mesa");
    mostrarToast(`Mesa ${mesa.numero} eliminada`);
  }, "No fue posible eliminar la mesa.");
  if (ok) await volverAMesas();
  else cerrarModal("overlay-eliminar-mesa");
}
