/* ==========================================================================
   INVENTARIO.JS — Módulo Inventario conectado a la API (vía Negocio)
   --------------------------------------------------------------------------
   · Consultar: cualquier usuario autenticado.
   · Crear / editar / eliminar / ajustar stock: solo con permiso "inventario.gestionar"
     (el Backend lo vuelve a validar; aquí solo se ocultan los botones).
   ========================================================================== */

let productos = [];
let categorias = [];
let proveedores = [];
let textoBusqueda = "";
let categoriaFiltro = "todas";
let productoEditando = null;   // producto en edición (null = alta)
let productoEliminando = null;
let guardando = false;

const $ = (id) => document.getElementById(id);
const puedeGestionar = () => Auth.tiene("inventario.gestionar");

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();

  $("buscar-inventario").addEventListener("input", (e) => { textoBusqueda = e.target.value; renderInventario(); });
  $("filtro-categoria").addEventListener("change", (e) => { categoriaFiltro = e.target.value; renderInventario(); });

  $("btn-nuevo-producto").addEventListener("click", abrirAlta);
  $("btn-guardar-producto").addEventListener("click", guardarProducto);
  $("btn-confirmar-eliminar-producto").addEventListener("click", confirmarEliminar);

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

  /* Si escribe una categoría nueva, la lista desplegable deja de ser obligatoria */
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

/* ---------- Utilidades de modal / aviso ---------- */
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

/* ---------- Carga (GET) ---------- */
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

/* ---------- Render ---------- */
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
        <td>${esc(p.nombre)}</td>
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

const codigoProducto = (p) => "PRD-" + String(p.idProducto).padStart(4, "0");

/* ---------- Validación del formulario ---------- */
function marcarError(idCampo, hayError) {
  const el = $(idCampo);
  const campo = el.closest(".campo");
  if (!campo) return;
  campo.classList.toggle("campo-error", hayError);
  const msg = campo.querySelector(".campo-mensaje-error");
  if (msg) msg.classList.toggle("campo-mensaje-error--visible", hayError);
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

const esEntero = (v) => /^\d+$/.test(String(v).trim());
const esDecimalValido = (v) => v !== "" && Number.isFinite(Number(v)) && Number(v) >= 0 && Number(v) <= 99999999.99;

/* ---------- Alta / edición ---------- */
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
  $("bloque-alta").hidden = true;
  $("bloque-edicion").hidden = false;
  abrirModal("overlay-nuevo-producto");
  $("input-nombre-producto").focus();
}

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
  chequeo("input-precio-producto", esDecimalValido(precioTxt));
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
    /* Una compra necesita proveedor y costo a la vez */
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
    /* Categoría: la nueva se crea (o se reutiliza si ya existe con ese nombre) */
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
      const filas = Confirmar.cambios(
        { n: productoEditando.nombre, c: productoEditando.categoria, p: productoEditando.precio, m: productoEditando.stockMinimo, s: productoEditando.stock },
        { n: nombre, c: catNombre, p: base.precio, m: base.stockMinimo, s: stockActual },
        { n: "Nombre", c: "Categoría", p: "Precio de venta", m: "Stock mínimo", s: "Stock (ajuste manual)" });
      if (!filas.length) { cerrarModal("overlay-nuevo-producto"); mostrarToast("No hay cambios para guardar."); return; }
      const ok = await Confirmar.pedir({ titulo: "Confirmar edición", mensaje: "Se modificará el producto <strong>" + esc(productoEditando.nombre) + "</strong>. ¿Desea guardar los cambios?", filas, textoConfirmar: "Sí, guardar cambios" });
      if (!ok) return;
      await Negocio.actualizarProducto(productoEditando, { ...base, activo: true });
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
    /* Si se creó la categoría y falló lo demás, refrescar para que aparezca */
    Negocio.categorias().then((c) => { categorias = c; poblarSelects(); }).catch(() => {});
  } finally {
    guardando = false;
    btn.disabled = false;
    btn.textContent = textoBtn;
  }
}

/* ---------- Eliminar ---------- */
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
    mostrarToast(Api.mensajeError(err, "No fue posible eliminar el producto."), true);
  } finally {
    guardando = false;
    btn.disabled = false;
  }
}
