/**
 * @file inventario.js
 * @description Módulo Inventario: productos, stock y categorías.
 *   - Consulta: cualquier usuario autenticado.
 *   - Crear, editar, eliminar, ajustar stock y administrar categorías: requiere "inventario.gestionar".
 *   - El ajuste manual de stock se registra como movimiento AJUSTE en Kardex.
 *   - Un producto con movimientos o ventas no puede eliminarse; se marca como Inactivo para que no se venda.
 *   Precios y costos en pesos colombianos (COP), sin centavos.
 * @requires api-negocio.js, data.js, auth.js
 */

/** Estado de la pantalla. */
let productos = [];
let categorias = [];
let proveedores = [];
let textoBusqueda = "";
let categoriaFiltro = "todas";
let productoEditando = null;
let productoEliminando = null;
let guardando = false;

const $ = (id) => document.getElementById(id);

/** Permiso de escritura en el inventario. */
const puedeGestionar = () => Auth.tiene("inventario.gestionar");

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  $("buscar-inventario").addEventListener("input", (e) => { textoBusqueda = e.target.value; renderInventario(); });
  $("filtro-categoria").addEventListener("change", (e) => { categoriaFiltro = e.target.value; renderInventario(); });

  $("btn-nuevo-producto").addEventListener("click", abrirAlta);
  $("btn-guardar-producto").addEventListener("click", guardarProducto);
  $("btn-confirmar-eliminar-producto").addEventListener("click", confirmarEliminar);
  $("btn-categorias").addEventListener("click", abrirCategorias);
  $("btn-crear-categoria").addEventListener("click", crearCategoriaDesdeModal);
  $("input-categoria-alta").addEventListener("keydown", (e) => { if (e.key === "Enter") crearCategoriaDesdeModal(); });
  $("tabla-categorias").addEventListener("click", manejarAccionCategoria);

  document.querySelectorAll("[data-cerrar]").forEach((btn) => {
    btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
  });

  $("tabla-inventario").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-accion]");
    if (!btn) return;
    const p = productos.find((x) => x.idProducto === Number(btn.dataset.id));
    if (!p) return;
    if (btn.dataset.accion === "editar") abrirEdicion(p);
    if (btn.dataset.accion === "eliminar") abrirEliminar(p);
  });

  // Escribir una categoría nueva hace opcional la selección de una existente.
  $("input-categoria-nueva").addEventListener("input", () => {
    if ($("input-categoria-nueva").value.trim()) $("input-categoria-producto").value = "";
  });

  Auth.listo.then(() => {
    const gestiona = puedeGestionar();
    $("btn-nuevo-producto").hidden = !gestiona;
    $("th-acciones").hidden = !gestiona;
    $("subtitulo-inventario").textContent = gestiona
      ? "Gestione productos, niveles de existencias y precios"
      : "Consulte productos, niveles de existencias y precios";
    cargarTodo();
  });
});

/* ---------- Ventanas y avisos ---------- */
function abrirModal(id) { $(id).classList.add("overlay--visible"); }
function cerrarModal(id) { $(id).classList.remove("overlay--visible"); }

function mostrarToast(mensaje, esError) {
  const toast = $("toast");
  toast.textContent = mensaje;
  toast.classList.toggle("toast--error", !!esError);
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), esError ? 4500 : 2500);
}

/* ---------- Carga ---------- */

/** Carga productos, categorías y, con permiso de gestión, proveedores. */
function cargarTodo() {
  $("tabla-inventario").innerHTML = filaMensaje("Cargando inventario...");
  return Promise.all([
    Negocio.productos(),
    Negocio.categorias(),
    puedeGestionar() ? Negocio.proveedores() : Promise.resolve([])
  ]).then(([prods, cats, provs]) => {
    productos = prods;
    categorias = cats;
    proveedores = provs;
    poblarSelects();
    renderInventario();
  }).catch((err) => {
    $("tabla-inventario").innerHTML = filaMensaje(Api.mensajeError(err, "No fue posible cargar el inventario. Intente de nuevo."));
  });
}

function filaMensaje(texto) {
  const cols = puedeGestionar() ? 9 : 8;
  return `<tr><td colspan="${cols}" class="col-centro">${esc(texto)}</td></tr>`;
}

function poblarSelects() {
  const nombres = [...new Set([
    ...categorias.map((c) => c.nombreCategoria),
    ...productos.map((p) => p.categoria)
  ].filter(Boolean))].sort((a, b) => a.localeCompare(b));

  const filtro = $("filtro-categoria");
  const previo = categoriaFiltro;
  filtro.innerHTML = `<option value="todas">Todas las Categorías</option>` +
    nombres.map((c) => `<option value="${esc(c)}">${esc(c)}</option>`).join("");
  filtro.value = nombres.includes(previo) ? previo : "todas";
  categoriaFiltro = filtro.value;

  $("input-categoria-producto").innerHTML = `<option value="">Selecciona una categoría</option>` +
    categorias.map((c) => `<option value="${c.idCategoria}">${esc(c.nombreCategoria)}</option>`).join("");

  $("input-proveedor-producto").innerHTML = `<option value="">Sin proveedor</option>` +
    proveedores.map((p) => `<option value="${p.idProveedor}">${esc(p.nombreProveedor || p.nombre || ("Proveedor " + p.idProveedor))}</option>`).join("");
}

/* ---------- Tabla e indicadores ---------- */

/** Dibuja indicadores (valor del inventario, stock bajo, sin stock) y la tabla filtrada. */
function renderInventario() {
  const valorTotal = productos.reduce((a, p) => a + p.costo * p.stock, 0);
  const sinStock = productos.filter((p) => p.stock <= 0).length;
  const stockBajo = productos.filter((p) => p.stock > 0 && p.stock <= p.stockMinimo).length;

  $("kpi-valor-total").textContent = formatoCOP(valorTotal);
  $("kpi-stock-bajo").textContent = stockBajo;
  $("kpi-sin-stock").textContent = sinStock;

  const texto = textoBusqueda.trim().toLowerCase();
  const visibles = productos.filter((p) => {
    const codigo = codigoProducto(p).toLowerCase();
    const okTexto = !texto || p.nombre.toLowerCase().includes(texto) || codigo.includes(texto);
    const okCat = categoriaFiltro === "todas" || p.categoria === categoriaFiltro;
    return okTexto && okCat;
  }).sort((a, b) => a.nombre.localeCompare(b.nombre));

  const gestiona = puedeGestionar();
  const cuerpo = $("tabla-inventario");

  if (!visibles.length) {
    cuerpo.innerHTML = filaMensaje(productos.length ? "No hay productos que coincidan con la búsqueda." : "Aún no hay productos registrados.");
  } else {
    cuerpo.innerHTML = visibles.map((p) => {
      let claseStock = "stock-valor";
      let alerta = "";
      if (p.stock <= 0) { claseStock += " stock-valor--sin"; alerta = `<span class="stock-alerta" title="Sin existencias">&#9888;</span>`; }
      else if (p.stock <= p.stockMinimo) { claseStock += " stock-valor--bajo"; alerta = `<span class="stock-alerta" title="Stock bajo">&#9888;</span>`; }

      const acciones = gestiona ? `
        <td class="col-centro">
          <div class="acciones-inv">
            <button type="button" class="btn-fila" data-accion="editar" data-id="${p.idProducto}">Editar</button>
            <button type="button" class="btn-fila btn-fila--peligro" data-accion="eliminar" data-id="${p.idProducto}">Eliminar</button>
          </div>
        </td>` : "";

      return `<tr>
        <td><div class="celda-producto-img">${esc(p.nombre.trim().charAt(0).toUpperCase())}</div></td>
        <td class="col-mono">${esc(codigoProducto(p))}</td>
        <td>${esc(p.nombre)}${p.activo ? "" : ` <span class="tag-inactivo" title="No se puede vender en Mesas">Inactivo</span>`}</td>
        <td>${esc(p.categoria)}</td>
        <td class="col-derecha">${formatoCOP(p.costo)}</td>
        <td class="col-derecha">${formatoCOP(p.precio)}</td>
        <td class="col-derecha"><span class="${claseStock}">${alerta} ${p.stock}</span></td>
        <td>${esc(p.proveedor || "—")}</td>
        ${acciones}
      </tr>`;
    }).join("");
  }

  $("tabla-pie").textContent = `Mostrando ${visibles.length} de ${productos.length} productos`;
}

/** Código visible del producto: PRD-0007. */
const codigoProducto = (p) => "PRD-" + String(p.idProducto).padStart(4, "0");

/* ---------- Validación ---------- */

/**
 * Marca o desmarca el error de un campo.
 * @param {string} idCampo
 * @param {boolean} hayError
 * @param {string} [mensaje] Reemplaza el mensaje por defecto del campo.
 */
function marcarError(idCampo, hayError, mensaje) {
  const el = $(idCampo);
  const campo = el.closest(".campo");
  if (!campo) return;
  campo.classList.toggle("campo-error", hayError);
  const msg = campo.querySelector(".campo-mensaje-error");
  if (msg) {
    if (mensaje) msg.textContent = mensaje;
    msg.classList.toggle("campo-mensaje-error--visible", hayError);
  }
}

function limpiarErrores() {
  document.querySelectorAll("#overlay-nuevo-producto .campo-error").forEach((el) => el.classList.remove("campo-error"));
  document.querySelectorAll("#overlay-nuevo-producto .campo-mensaje-error").forEach((el) => {
    if (el.id !== "error-producto") el.classList.remove("campo-mensaje-error--visible");
  });
  const g = $("error-producto");
  g.textContent = "";
  g.hidden = true;
}

function errorGeneral(texto) {
  const g = $("error-producto");
  g.textContent = texto;
  g.hidden = !texto;
}

/** Límites del Backend: enteros de 32 bits y precios en pesos enteros hasta 99 999 999. */
const MAX_INT = 2147483647;
const esEntero = (v) => /^\d+$/.test(String(v).trim()) && Number(v) <= MAX_INT;
const esDecimalValido = (v) => /^\d+$/.test(String(v).trim()) && Number(v) <= 99999999;

/* ---------- Alta y edición ---------- */
function abrirAlta() {
  productoEditando = null;
  limpiarErrores();
  $("titulo-producto").textContent = "Registrar Nuevo Producto";
  ["input-nombre-producto", "input-categoria-nueva", "input-precio-producto", "input-stock-minimo",
    "input-stock-inicial", "input-costo-producto", "input-stock-actual"].forEach((id) => { $(id).value = ""; });
  $("input-categoria-producto").value = "";
  $("input-proveedor-producto").value = "";
  $("bloque-alta").hidden = false;
  $("bloque-edicion").hidden = true;
  $("input-categoria-nueva").closest(".campo").hidden = false;
  abrirModal("overlay-nuevo-producto");
  $("input-nombre-producto").focus();
}

function abrirEdicion(p) {
  productoEditando = p;
  limpiarErrores();
  $("titulo-producto").textContent = "Editar Producto";
  $("input-nombre-producto").value = p.nombre;
  $("input-categoria-producto").value = p.idCategoria ? String(p.idCategoria) : "";
  $("input-categoria-nueva").value = "";
  $("input-precio-producto").value = p.precio;
  $("input-stock-minimo").value = p.stockMinimo;
  $("input-stock-actual").value = p.stock;
  $("input-estado-producto").value = p.activo ? "activo" : "inactivo";
  $("bloque-alta").hidden = true;
  $("bloque-edicion").hidden = false;
  abrirModal("overlay-nuevo-producto");
  $("input-nombre-producto").focus();
}

/**
 * Valida y guarda el producto. En la edición conserva el estado elegido (activo o inactivo)
 * y registra un AJUSTE en Kardex si cambia el stock.
 */
async function guardarProducto() {
  if (guardando) return;
  if (!puedeGestionar()) { mostrarToast("No tiene permiso para modificar el inventario.", true); return; }
  limpiarErrores();

  const nombre = $("input-nombre-producto").value.trim();
  const catNueva = $("input-categoria-nueva").value.trim();
  const catSel = $("input-categoria-producto").value;
  const precioTxt = $("input-precio-producto").value.trim();
  const minTxt = $("input-stock-minimo").value.trim();
  const editando = !!productoEditando;

  let ok = true;
  const chequeo = (id, cond) => { marcarError(id, !cond); if (!cond) ok = false; };

  chequeo("input-nombre-producto", nombre.length > 0 && nombre.length <= 200);
  chequeo("input-categoria-producto", (!!catSel || (catNueva.length > 0 && catNueva.length <= 100)));
  if (!precioTxt) { marcarError("input-precio-producto", true, "El precio de venta es obligatorio."); ok = false; }
  else if (!esDecimalValido(precioTxt)) { marcarError("input-precio-producto", true, "Ingresa un precio en pesos (COP), sin centavos, entre 0 y 99.999.999."); ok = false; }
  else marcarError("input-precio-producto", false);
  chequeo("input-stock-minimo", minTxt === "" || esEntero(minTxt));

  let stockInicial = 0, costoTxt = "", idProveedor = null, stockActual = null;
  if (editando) {
    const sTxt = $("input-stock-actual").value.trim();
    chequeo("input-stock-actual", esEntero(sTxt));
    if (esEntero(sTxt)) stockActual = Number(sTxt);
  } else {
    const siTxt = $("input-stock-inicial").value.trim();
    costoTxt = $("input-costo-producto").value.trim();
    idProveedor = $("input-proveedor-producto").value ? Number($("input-proveedor-producto").value) : null;
    chequeo("input-stock-inicial", siTxt === "" || esEntero(siTxt));
    stockInicial = siTxt === "" ? 0 : Number(siTxt);
    chequeo("input-costo-producto", costoTxt === "" || esDecimalValido(costoTxt));

    // El stock inicial se registra como compra solo si se indican proveedor y costo a la vez.
    if (stockInicial > 0 && (idProveedor && costoTxt === "")) { marcarError("input-costo-producto", true); ok = false; }
    if (stockInicial > 0 && (!idProveedor && costoTxt !== "")) { marcarError("input-proveedor-producto", true); ok = false; }
  }
  if (!ok) return;

  guardando = true;
  const btn = $("btn-guardar-producto");
  const textoBtn = btn.textContent;
  btn.disabled = true;
  btn.textContent = "Guardando...";

  try {
    // Una categoría nueva se crea, salvo que ya exista otra con el mismo nombre.
    let idCategoria = catSel ? Number(catSel) : 0;
    if (!idCategoria) {
      const existente = categorias.find((c) => c.nombreCategoria.trim().toLowerCase() === catNueva.toLowerCase());
      if (existente) idCategoria = existente.idCategoria;
      else {
        const nueva = await Negocio.crearCategoria(catNueva);
        idCategoria = nueva && nueva.idCategoria;
        if (!idCategoria) throw { status: 500, mensaje: "El servidor no devolvió la categoría creada." };
      }
    }

    const base = { nombre, precio: Number(precioTxt), stockMinimo: minTxt === "" ? 0 : Number(minTxt), idCategoria };
    let mensaje;
    let avisos = [];

    if (editando) {
      const catNombre = (categorias.find((c) => c.idCategoria === idCategoria) || {}).nombreCategoria || "";
      const activo = $("input-estado-producto").value === "activo";
      const filas = Confirmar.cambios(
        { n: productoEditando.nombre, c: productoEditando.categoria, p: productoEditando.precio, m: productoEditando.stockMinimo, s: productoEditando.stock, e: productoEditando.activo ? "Activo" : "Inactivo" },
        { n: nombre, c: catNombre, p: base.precio, m: base.stockMinimo, s: stockActual, e: activo ? "Activo" : "Inactivo" },
        { n: "Nombre", c: "Categoría", p: "Precio de venta", m: "Stock mínimo", s: "Stock (ajuste manual)", e: "Estado" });
      if (!filas.length) { cerrarModal("overlay-nuevo-producto"); mostrarToast("No hay cambios para guardar."); return; }
      const ok = await Confirmar.pedir({ titulo: "Confirmar edición", mensaje: "Se modificará el producto <strong>" + esc(productoEditando.nombre) + "</strong>. ¿Desea guardar los cambios?", filas, textoConfirmar: "Sí, guardar cambios" });
      if (!ok) return;
      await Negocio.actualizarProducto(productoEditando, { ...base, activo });
      if (stockActual !== productoEditando.stock) {
        await Negocio.ajustarStock(productoEditando.idProducto, stockActual, "Ajuste manual desde Inventario");
      }
      mensaje = `Producto "${nombre}" actualizado.`;
    } else {
      const r = await Negocio.crearProducto({
        ...base, stockInicial, idProveedor, costo: costoTxt === "" ? undefined : Number(costoTxt)
      });
      avisos = r.aviso || [];
      mensaje = `Producto "${nombre}" registrado.`;
    }

    cerrarModal("overlay-nuevo-producto");
    await cargarTodo();
    mostrarToast(avisos.length ? avisos.join(" ") : mensaje, avisos.length > 0);
  } catch (err) {
    errorGeneral(Api.mensajeError(err, "No fue posible guardar el producto. Intente de nuevo."));
    Negocio.categorias().then((c) => { categorias = c; poblarSelects(); }).catch(() => {});
  } finally {
    guardando = false;
    btn.disabled = false;
    btn.textContent = textoBtn;
  }
}

/* ---------- Eliminación ---------- */
function abrirEliminar(p) {
  productoEliminando = p;
  $("texto-eliminar-producto").textContent =
    `¿Está seguro de que desea eliminar "${p.nombre}"? Si tiene ventas o movimientos registrados, el sistema podría rechazar la eliminación.`;
  abrirModal("overlay-eliminar-producto");
}

async function confirmarEliminar() {
  if (!productoEliminando || guardando) return;
  guardando = true;
  const btn = $("btn-confirmar-eliminar-producto");
  btn.disabled = true;
  try {
    const nombre = productoEliminando.nombre;
    await Negocio.eliminarProducto(productoEliminando);
    cerrarModal("overlay-eliminar-producto");
    productoEliminando = null;
    await cargarTodo();
    mostrarToast(`Producto "${nombre}" eliminado.`);
  } catch (err) {
    cerrarModal("overlay-eliminar-producto");
    mostrarToast(err && err.status === 409
      ? `No se puede eliminar "${productoEliminando.nombre}" porque ya tiene movimientos de inventario, compras o ventas. Edítelo y márquelo como Inactivo para que no se venda.`
      : Api.mensajeError(err, "No fue posible eliminar el producto."), true);
  } finally {
    guardando = false;
    btn.disabled = false;
  }
}

/* ==================== Categorías ====================
   Consulta para todos los usuarios; crear, renombrar y eliminar requiere "inventario.gestionar".
   El Backend no permite eliminar una categoría con productos (409). */
let categoriaEditando = null;

const productosDeCategoria = (idCategoria) => productos.filter((p) => p.idCategoria === idCategoria).length;

function abrirCategorias() {
  categoriaEditando = null;
  const gestiona = puedeGestionar();
  $("bloque-categoria-nueva").hidden = !gestiona;
  $("th-acciones-categorias").hidden = !gestiona;
  $("descripcion-categorias").textContent = gestiona
    ? "Cree, renombre o elimine categorías. Se muestra cuántos productos tiene cada una."
    : "Categorías existentes y cuántos productos tiene cada una.";
  $("input-categoria-alta").value = "";
  errorCategoria("");
  renderCategorias();
  abrirModal("overlay-categorias");
}

function errorCategoria(texto) {
  const el = $("error-categoria-alta");
  el.textContent = texto;
  el.classList.toggle("campo-mensaje-error--visible", !!texto);
  $("input-categoria-alta").classList.toggle("campo-error", !!texto);
}

/** Dibuja la tabla de categorías con su identificador y el número de productos. */
function renderCategorias() {
  const gestiona = puedeGestionar();
  const lista = [...categorias].sort((a, b) => a.idCategoria - b.idCategoria);
  const cuerpo = $("tabla-categorias");
  if (!lista.length) {
    cuerpo.innerHTML = `<tr><td colspan="${gestiona ? 4 : 3}" class="col-centro">Aún no hay categorías.</td></tr>`;
    return;
  }
  cuerpo.innerHTML = lista.map((c) => {
    const n = productosDeCategoria(c.idCategoria);
    const editando = categoriaEditando === c.idCategoria;
    const nombre = editando
      ? `<input type="text" maxlength="100" value="${esc(c.nombreCategoria)}" data-input-categoria="${c.idCategoria}" aria-label="Nuevo nombre de la categoría">`
      : esc(c.nombreCategoria);
    let acciones = "";
    if (gestiona) {
      acciones = editando
        ? `<td class="col-centro"><div class="acciones-categoria">
             <button type="button" class="btn-fila" data-cat-guardar="${c.idCategoria}">Guardar</button>
             <button type="button" class="btn-fila" data-cat-cancelar="${c.idCategoria}">Cancelar</button></div></td>`
        : `<td class="col-centro"><div class="acciones-categoria">
             <button type="button" class="btn-fila" data-cat-editar="${c.idCategoria}">Renombrar</button>
             <button type="button" class="btn-fila btn-fila--peligro" data-cat-eliminar="${c.idCategoria}" ${n ? `disabled title="Tiene ${n} producto(s): cámbielos de categoría primero"` : ""}>Eliminar</button></div></td>`;
    }
    return `<tr><td class="col-id">#${c.idCategoria}</td><td>${nombre}</td><td class="col-centro">${n}</td>${acciones}</tr>`;
  }).join("");
  const input = cuerpo.querySelector("[data-input-categoria]");
  if (input) {
    input.focus();
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") guardarNombreCategoria(categoriaEditando);
      if (e.key === "Escape") { e.stopPropagation(); categoriaEditando = null; renderCategorias(); }
    });
  }
}

async function recargarCategorias() {
  categorias = await Negocio.categorias();
  poblarSelects();
  renderCategorias();
}

/** Indica si ya existe otra categoría con el mismo nombre (sin distinguir mayúsculas). */
const nombreRepetido = (nombre, exceptoId) =>
  categorias.some((c) => c.idCategoria !== exceptoId && c.nombreCategoria.trim().toLowerCase() === nombre.toLowerCase());

async function crearCategoriaDesdeModal() {
  if (guardando || !puedeGestionar()) return;
  const nombre = $("input-categoria-alta").value.trim();
  if (!nombre) { errorCategoria("Escriba el nombre de la categoría."); return; }
  if (nombre.length > 100) { errorCategoria("El nombre no puede superar 100 caracteres."); return; }
  if (nombreRepetido(nombre)) { errorCategoria("Ya existe una categoría con ese nombre."); return; }
  errorCategoria("");
  guardando = true;
  try {
    await Negocio.crearCategoria(nombre);
    $("input-categoria-alta").value = "";
    await recargarCategorias();
    mostrarToast(`Categoría "${nombre}" creada.`);
  } catch (err) {
    errorCategoria(Api.mensajeError(err, "No fue posible crear la categoría."));
  } finally {
    guardando = false;
  }
}

async function guardarNombreCategoria(idCategoria) {
  if (guardando) return;
  const c = categorias.find((x) => x.idCategoria === idCategoria);
  const input = document.querySelector(`[data-input-categoria="${idCategoria}"]`);
  if (!c || !input) return;
  const nombre = input.value.trim();
  if (!nombre || nombre.length > 100) { mostrarToast("El nombre es obligatorio (máximo 100 caracteres).", true); return; }
  if (nombre === c.nombreCategoria) { categoriaEditando = null; renderCategorias(); return; }
  if (nombreRepetido(nombre, idCategoria)) { mostrarToast("Ya existe otra categoría con ese nombre.", true); return; }
  guardando = true;
  try {
    await Negocio.actualizarCategoria(c, nombre);
    categoriaEditando = null;
    await recargarCategorias();
    await cargarTodo();
    mostrarToast(`Categoría renombrada a "${nombre}".`);
  } catch (err) {
    mostrarToast(Api.mensajeError(err, "No fue posible renombrar la categoría."), true);
  } finally {
    guardando = false;
  }
}

/** Elimina una categoría sin productos, previa confirmación. */
async function eliminarCategoriaConfirmada(idCategoria) {
  const c = categorias.find((x) => x.idCategoria === idCategoria);
  if (!c || guardando) return;
  const n = productosDeCategoria(idCategoria);
  if (n) { mostrarToast(`No se puede eliminar: "${c.nombreCategoria}" tiene ${n} producto(s).`, true); return; }
  const ok = await Confirmar.pedir({
    titulo: "Eliminar categoría",
    mensaje: "¿Desea eliminar la categoría <strong>" + esc(c.nombreCategoria) + "</strong> (ID " + c.idCategoria + ")? Esta acción no se puede deshacer.",
    textoConfirmar: "Sí, eliminar", peligro: true
  });
  if (!ok) return;
  guardando = true;
  try {
    await Negocio.eliminarCategoria(c);
    await recargarCategorias();
    mostrarToast(`Categoría "${c.nombreCategoria}" eliminada.`);
  } catch (err) {
    mostrarToast(err && err.status === 409
      ? `No se puede eliminar "${c.nombreCategoria}" porque todavía tiene productos asociados.`
      : Api.mensajeError(err, "No fue posible eliminar la categoría."), true);
  } finally {
    guardando = false;
  }
}

/** Acciones de la tabla de categorías: renombrar, guardar, cancelar y eliminar. */
function manejarAccionCategoria(e) {
  const b = e.target.closest("button");
  if (!b || b.disabled) return;
  if (b.dataset.catEditar) { categoriaEditando = Number(b.dataset.catEditar); renderCategorias(); }
  else if (b.dataset.catCancelar) { categoriaEditando = null; renderCategorias(); }
  else if (b.dataset.catGuardar) guardarNombreCategoria(Number(b.dataset.catGuardar));
  else if (b.dataset.catEliminar) eliminarCategoriaConfirmada(Number(b.dataset.catEliminar));
}
