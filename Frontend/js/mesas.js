/**
 * @file mesas.js
 * @description Módulo Mesas: salón, cuenta de cada mesa, cobro y comprobante.
 *   Ciclo de una mesa: Libre → Ocupada (al agregar productos) → Cerrando (al cerrar la cuenta)
 *   → Libre (al registrar el pago).
 *   - Mesero y caja agregan o quitan productos y cierran la cuenta.
 *   - El cobro requiere "cobros.gestionar": muestra la vista previa del comprobante, registra con qué
 *     pagó el cliente y emite el comprobante para imprimir.
 *   - Crear, eliminar, retirar y reactivar mesas requiere "seguridad.gestionar".
 *     Una mesa con cuentas asociadas no puede eliminarse; en su lugar se retira (estado Inactiva).
 * @requires api-negocio.js, comprobante.js, data.js, auth.js
 */

/** Estado de la pantalla. */
let filtroActual = "todas";
let mesas = [];
let catalogo = [];
let mesaSeleccionada = null;
let detalle = null;

/** Evita ejecutar dos operaciones a la vez por clics repetidos. */
let ocupado = false;

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
  $("btn-imprimir-comprobante").addEventListener("click", () => { if (comprobanteEmitido) Comprobante.imprimir(comprobanteEmitido); });
  $("btn-cerrar-comprobante").addEventListener("click", () => cerrarModal("overlay-cobro"));
  $("select-metodo-pago").addEventListener("change", () => { if (!comprobanteEmitido) renderVistaPrevia(); });
  $("lista-mesas-retiradas").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-reactivar]");
    if (btn) reactivarMesa(Number(btn.dataset.reactivar));
  });
  $("btn-agregar-metodo").addEventListener("click", agregarMetodoPago);
  $("btn-eliminar-mesa").addEventListener("click", () => abrirModal("overlay-eliminar-mesa"));
  $("btn-confirmar-eliminar-mesa").addEventListener("click", eliminarMesa);

  $("detalle-items").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (btn) manejarAccionItem(btn.dataset.accion, Number(btn.dataset.id));
  });

  Auth.listo.then(() => {
    // Solo la administración crea o elimina mesas.
    $("btn-nueva-mesa").hidden = !Auth.esAdmin();
    cargarMesas();
  });
});

/* ---------- Ventanas y avisos ---------- */
function abrirModal(id) { $(id).classList.add("overlay--visible"); }
function cerrarModal(id) {
  $(id).classList.remove("overlay--visible");

  // Al cerrar un comprobante ya emitido la mesa está libre: se vuelve al salón.
  if (id === "overlay-cobro" && comprobanteEmitido) {
    comprobanteEmitido = null;
    volverAMesas();
  }
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

/**
 * Ejecuta una operación asíncrona sin solaparla con otra y muestra el mensaje de error del Backend.
 * @param {Function} operacion
 * @param {string} [textoError] Mensaje si el Backend no envía uno.
 * @returns {Promise<boolean>} true si terminó sin error.
 */
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

/* ---------- Salón ---------- */
async function cargarMesas() {
  $("mesas-grid").innerHTML = `<p class="picker-vacio">Cargando mesas...</p>`;
  try {
    mesas = await Negocio.cargarMesas();
    renderMesas();
    /* Corrige en segundo plano las mesas marcadas como ocupadas sin consumo. */
    Negocio.sincronizarEstadosMesas(mesas).catch(() => {});
  } catch (err) {
    $("mesas-grid").innerHTML = `<p class="picker-vacio">${esc(Api.mensajeError(err, "No fue posible cargar las mesas."))}</p>`;
  }
}

/** Dibuja el salón y los contadores. Las mesas retiradas no se muestran ni se cuentan. */
function renderMesas() {
  const enSalon = mesas.filter(m => m.estado !== "inactiva");
  const total = enSalon.length;
  const conteo = { libre: 0, ocupada: 0, cerrando: 0 };
  enSalon.forEach(m => { if (conteo[m.estado] !== undefined) conteo[m.estado]++; });
  renderMesasRetiradas();

  $("conteo-ocupadas").innerHTML = `${conteo.ocupada} <small>/ ${total}</small>`;
  $("conteo-libres").innerHTML = `${conteo.libre} <small>/ ${total}</small>`;
  $("conteo-cerrando").innerHTML = `${conteo.cerrando} <small>/ ${total}</small>`;

  const visibles = enSalon.filter(m => filtroActual === "todas" || m.estado === filtroActual);
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

/** Lista de mesas retiradas, visible solo para la administración. */
function renderMesasRetiradas() {
  const retiradas = mesas.filter(m => m.estado === "inactiva");
  const bloque = $("bloque-mesas-retiradas");
  bloque.hidden = !(Auth.esAdmin() && retiradas.length);
  $("lista-mesas-retiradas").innerHTML = retiradas.map(m => `
    <div class="mesa-retirada">
      <span>Mesa ${esc(m.numero)}</span>
      <button type="button" class="btn btn--secundario" data-reactivar="${m.idMesa}">Reactivar</button>
    </div>`).join("");
}

async function reactivarMesa(idMesa) {
  const mesa = mesas.find(m => m.idMesa === idMesa);
  if (!mesa) return;
  const ok = await ejecutar(() => Negocio.reactivarMesa(mesa), "No fue posible reactivar la mesa.");
  if (ok) { mostrarToast(`Mesa ${mesa.numero} reactivada`); await cargarMesas(); }
}

/* ---------- Alta de mesas ---------- */

/** Registra una mesa. Si el número pertenece a una mesa retirada, ofrece reactivarla en lugar de duplicarla. */
async function registrarNuevaMesa() {
  const input = $("input-numero-mesa");
  const error = $("error-numero-mesa");
  const numero = parseInt(input.value, 10);

  const retirada = mesas.find(m => m.numero === numero && m.estado === "inactiva");
  if (retirada) {
    const si = await Confirmar.pedir({
      titulo: "Mesa retirada",
      mensaje: "La mesa <strong>" + esc(numero) + "</strong> ya existe, pero está retirada del salón. ¿Desea reactivarla?",
      textoConfirmar: "Sí, reactivar"
    });
    if (si) { input.value = ""; cerrarModal("overlay-nueva-mesa"); await reactivarMesa(retirada.idMesa); }
    return;
  }
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

/* ---------- Cuenta de la mesa ---------- */
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

/** Dibuja la cuenta y habilita las acciones según el estado de la mesa y los permisos. */
function renderDetalle() {
  const mesa = mesaSeleccionada;
  if (!mesa || !detalle) return;

  $("detalle-titulo").textContent = `Mesa ${mesa.numero}`;
  const conConsumo = detalle.items.length > 0;
  $("detalle-meta").innerHTML = `&#128337; ${mesa.cuenta && conConsumo ? "Abierta desde " + horaDe(mesa.cuenta.fechaApertura) : "Mesa libre: agregue productos para abrir la cuenta"}`;

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
  $("btn-confirmar-pago").textContent = puedeCobrar ? "Cerrar cuenta y cobrar" : "Cerrar cuenta (enviar a caja)";
  $("btn-marcar-pagada").textContent = "Cobrar y emitir comprobante";
  $("btn-confirmar-pago").style.display = cerrando ? "none" : "block";
  $("btn-confirmar-pago").disabled = detalle.items.length === 0;
  $("btn-marcar-pagada").style.display = cerrando && puedeCobrar ? "block" : "none";
  $("btn-reabrir-cuenta").style.display = cerrando ? "block" : "none";
  $("nota-cobro-caja").style.display = cerrando && !puedeCobrar ? "block" : "none";
  $("btn-agregar-productos").disabled = cerrando;
  $("btn-agregar-productos").style.opacity = cerrando ? "0.5" : "1";

  // Una mesa solo puede eliminarse estando libre y sin consumo.
  $("btn-eliminar-mesa").hidden = !(Auth.esAdmin() && mesa.estado === "libre" && !conConsumo);
}

/** Acciones de cada línea: sumar, restar o quitar (esta última pide confirmación). */
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

/* ---------- Selector de productos ---------- */
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

/** Lista de productos activos; los que no tienen stock no se pueden agregar. */
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

/* ---------- Cierre de la cuenta ---------- */

/** Pasa la mesa a "Cerrando". Si el usuario puede cobrar, abre de inmediato el comprobante. */
async function confirmarPago() {
  const mesa = mesaSeleccionada;
  const ok = await ejecutar(
    () => Negocio.confirmarPago(mesa, detalle),
    "No fue posible confirmar el pago."
  );
  if (ok) {
    renderDetalle();
    if (Auth.tiene("cobros.gestionar")) abrirCobro();
    else mostrarToast(`Mesa ${mesa.numero} pasó a "Cerrando": pendiente de cobro en caja`);
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

/* ---------- Comprobante de pago ----------
   Paso 1: vista previa del comprobante y selección del método con el que pagó el cliente.
   Paso 2: registro del pago y comprobante emitido, con número y fecha de cierre. */
let metodos = [];

/** Comprobante registrado, disponible para imprimir. */
let comprobanteEmitido = null;

const nombreUsuarioSesion = () => {
  const el = document.querySelector(".header-usuario-nombre");
  const u = Auth.usuario();
  return (el && el.textContent.trim()) || (u && u.email) || "—";
};

/** Nombre de quien atendió la mesa (usuario del pedido). */
function nombreMesero() {
  const p = detalle && detalle.pedidos && detalle.pedidos[0];
  if (p && p.usuario) return `${p.usuario.nombres || ""} ${p.usuario.apellidos || ""}`.trim() || "—";
  return p ? `Usuario #${p.idUsuario}` : nombreUsuarioSesion();
}

/** Datos del comprobante a partir de la cuenta actual. */
function datosComprobante(extra) {
  const t = Negocio.totales(detalle.items);
  return Object.assign({
    numero: null, borrador: true,
    mesa: mesaSeleccionada ? mesaSeleccionada.numero : "—",
    atendio: nombreMesero(),
    cobro: nombreUsuarioSesion(),
    apertura: detalle.cuenta && detalle.cuenta.fechaApertura,
    cierre: null,
    metodo: "",
    items: detalle.items.map(i => ({ nombre: nombreProducto(i.idProducto), cantidad: i.cantidad, precioUnitario: i.precioUnitario })),
    subtotal: t.subtotal, iva: t.iva, total: t.total, tasaIva: Negocio.IVA
  }, extra || {});
}

function metodoSeleccionado() {
  const id = Number($("select-metodo-pago").value);
  const m = metodos.find(x => x.idMetodo === id);
  return m ? m.nombreMetodo : "";
}

function renderVistaPrevia() {
  $("cobro-comprobante").innerHTML = Comprobante.html(datosComprobante({ metodo: metodoSeleccionado() }));
}

/** Alterna la ventana entre vista previa (selección del método) y comprobante emitido. */
function modoCobro(emitido) {
  $("bloque-seleccion-pago").hidden = emitido;
  $("pie-cobro").hidden = emitido;
  $("pie-comprobante").hidden = !emitido;
  $("cobro-titulo").textContent = emitido ? "Comprobante emitido" : "Comprobante de pago";
  $("cobro-descripcion").textContent = emitido
    ? "El pago quedó registrado y la mesa está libre. Puede imprimir el comprobante."
    : "Revise la cuenta y seleccione con qué pagó el cliente.";
}

async function abrirCobro() {
  if (!detalle || !detalle.items.length) return;
  comprobanteEmitido = null;
  modoCobro(false);
  $("cobro-error").textContent = "";
  $("select-metodo-pago").innerHTML = `<option value="">Cargando métodos...</option>`;
  $("bloque-nuevo-metodo").hidden = true;
  renderVistaPrevia();
  abrirModal("overlay-cobro");
  await cargarMetodos();
  renderVistaPrevia();
}

/** Carga los métodos de pago. Si no hay ninguno, quien cobra puede crear uno desde la misma ventana. */
async function cargarMetodos() {
  try {
    metodos = await Negocio.metodosPago();
  } catch (err) {
    metodos = [];
    $("cobro-error").textContent = Api.mensajeError(err, "No fue posible cargar los métodos de pago.");
  }
  const select = $("select-metodo-pago");
  select.innerHTML = `<option value="">Seleccione con qué pagó</option>` +
    metodos.map(m => `<option value="${m.idMetodo}">${esc(m.nombreMetodo)}</option>`).join("");
  if (metodos.length === 1) select.value = String(metodos[0].idMetodo);

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
  if (ok) { await cargarMetodos(); renderVistaPrevia(); }
}

/** Registra el pago y muestra el comprobante emitido. */
async function confirmarCobro() {
  const idMetodo = Number($("select-metodo-pago").value);
  if (!idMetodo) { $("cobro-error").textContent = "Seleccione con qué pagó el cliente."; return; }
  $("cobro-error").textContent = "";

  const mesa = mesaSeleccionada;
  const metodo = metodoSeleccionado();
  const previo = datosComprobante({ metodo });
  let resultado = null;
  const ok = await ejecutar(async () => {
    resultado = await Negocio.cobrar(mesa, detalle, idMetodo);
  }, "No fue posible registrar el pago.");
  if (!ok) return;

  comprobanteEmitido = Object.assign(previo, {
    borrador: false,
    numero: resultado && resultado.idComprobante,
    apertura: (resultado && resultado.fechaApertura) || previo.apertura,
    cierre: (resultado && resultado.fechaCierre) || new Date(),
    subtotal: resultado ? resultado.subtotal : previo.subtotal,
    iva: resultado ? resultado.iva : previo.iva,
    total: resultado ? resultado.total : previo.total
  });
  $("cobro-comprobante").innerHTML = Comprobante.html(comprobanteEmitido);
  modoCobro(true);
  mostrarToast(`Pago registrado. Mesa ${mesa.numero} liberada`);
}

/* ---------- Eliminación de mesas ---------- */

/**
 * Elimina la mesa. Si el Backend responde 409 (tiene cuentas asociadas), ofrece retirarla del salón
 * para conservar su historial de ventas.
 */
async function eliminarMesa() {
  const mesa = mesaSeleccionada;
  let conHistorial = false;
  const ok = await ejecutar(async () => {
    try {
      await Negocio.eliminarMesa(mesa);
    } catch (err) {
      if (err && err.status === 409) { conHistorial = true; return; }
      throw err;
    }
    mostrarToast(`Mesa ${mesa.numero} eliminada`);
  }, "No fue posible eliminar la mesa.");
  cerrarModal("overlay-eliminar-mesa");
  if (!ok) return;

  if (conHistorial) {
    const retirar = await Confirmar.pedir({
      titulo: "La mesa tiene historial",
      mensaje: "La mesa <strong>" + esc(mesa.numero) + "</strong> ya tuvo cuentas, por eso no se puede borrar sin perder el historial de ventas. " +
        "¿Desea <strong>retirarla del salón</strong>? Dejará de aparecer, pero sus ventas se conservan y podrá reactivarla después.",
      textoConfirmar: "Sí, retirar mesa", peligro: true
    });
    if (!retirar) return;
    const okRetiro = await ejecutar(() => Negocio.retirarMesa(mesa), "No fue posible retirar la mesa.");
    if (!okRetiro) return;
    mostrarToast(`Mesa ${mesa.numero} retirada del salón`);
  }
  await volverAMesas();
}
