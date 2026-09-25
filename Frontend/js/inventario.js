/* ==========================================================================
   INVENTARIO.JS — Lógica funcional del módulo Inventario
   ========================================================================== */

let textoBusqueda = "";
let categoriaFiltro = "todas";
let imagenNuevoProducto = null; // data URL en sesión (si el archivo es liviano)

const LIMITE_IMAGEN_BYTES = 180 * 1024; // 180kb, para no saturar localStorage

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("fecha-actual").textContent = fechaHoy();

  renderInventario();

  document.getElementById("buscar-inventario").addEventListener("input", (e) => {
    textoBusqueda = e.target.value;
    renderInventario();
  });

  document.getElementById("filtro-categoria").addEventListener("change", (e) => {
    categoriaFiltro = e.target.value;
    renderInventario();
  });

  document.getElementById("btn-nuevo-producto").addEventListener("click", abrirModalNuevoProducto);
  document.querySelectorAll("[data-cerrar]").forEach(btn => {
    btn.addEventListener("click", () => cerrarModal(btn.dataset.cerrar));
  });

  document.getElementById("btn-guardar-producto").addEventListener("click", guardarNuevoProducto);
  document.getElementById("input-imagen-producto").addEventListener("change", manejarSeleccionImagen);
});

/* ---------- Utilidades de modal ---------- */
function abrirModal(id) { document.getElementById(id).classList.add("overlay--visible"); }
function cerrarModal(id) { document.getElementById(id).classList.remove("overlay--visible"); }

function mostrarToast(mensaje) {
  const toast = document.getElementById("toast");
  toast.textContent = mensaje;
  toast.classList.add("toast--visible");
  clearTimeout(mostrarToast._t);
  mostrarToast._t = setTimeout(() => toast.classList.remove("toast--visible"), 2200);
}

/* ---------- Render principal ---------- */
function categoriasUnicas() {
  const productos = obtenerProductos();
  return [...new Set(productos.map(p => p.categoria))].sort();
}

function poblarSelectCategorias() {
  const categorias = categoriasUnicas();

  const filtro = document.getElementById("filtro-categoria");
  const valorFiltroPrevio = filtro.value || "todas";
  filtro.innerHTML = `<option value="todas">Todas las Categorías</option>` +
    categorias.map(c => `<option value="${c}">${c}</option>`).join("");
  filtro.value = categorias.includes(valorFiltroPrevio) ? valorFiltroPrevio : "todas";

  const modal = document.getElementById("input-categoria-producto");
  modal.innerHTML = `<option value="">Selecciona una categoría</option>` +
    categorias.map(c => `<option value="${c}">${c}</option>`).join("");
}

function renderInventario() {
  poblarSelectCategorias();

  const productos = obtenerProductos();

  const valorTotal = productos.reduce((acc, p) => acc + p.costo * p.stock, 0);
  const stockBajo = productos.filter(p => p.stock > 0 && p.stock <= p.stockMinimo).length;
  const sinStock = productos.filter(p => p.stock === 0).length;

  document.getElementById("kpi-valor-total").textContent = formatoCOP(valorTotal);
  document.getElementById("kpi-stock-bajo").textContent = stockBajo;
  document.getElementById("kpi-sin-stock").textContent = sinStock;

  const texto = textoBusqueda.trim().toLowerCase();
  const visibles = productos.filter(p => {
    const coincideTexto = !texto || p.nombre.toLowerCase().includes(texto) || p.id.toLowerCase().includes(texto);
    const coincideCategoria = categoriaFiltro === "todas" || p.categoria === categoriaFiltro;
    return coincideTexto && coincideCategoria;
  });

  const cuerpo = document.getElementById("tabla-inventario");
  cuerpo.innerHTML = "";

  visibles.sort((a, b) => a.nombre.localeCompare(b.nombre)).forEach(p => {
    const tr = document.createElement("tr");

    let claseStock = "";
    let alerta = "";
    if (p.stock === 0) {
      claseStock = "stock-valor stock-valor--sin";
      alerta = `<span class="stock-alerta" title="Sin existencias">&#9888;</span>`;
    } else if (p.stock <= p.stockMinimo) {
      claseStock = "stock-valor stock-valor--bajo";
      alerta = `<span class="stock-alerta" title="Stock bajo">&#9888;</span>`;
    }

    const inicialAvatar = p.nombre.trim().charAt(0).toUpperCase();
    const imagenHTML = p.imagen
      ? `<img src="${p.imagen}" alt="${p.nombre}">`
      : inicialAvatar;

    tr.innerHTML = `
      <td><div class="celda-producto-img">${imagenHTML}</div></td>
      <td class="col-mono">${p.id}</td>
      <td>${p.nombre}</td>
      <td>${p.categoria}</td>
      <td class="col-derecha">${formatoCOP(p.costo)}</td>
      <td class="col-derecha">${formatoCOP(p.precio)}</td>
      <td class="col-derecha"><span class="${claseStock}">${alerta} ${p.stock}</span></td>
      <td>${p.proveedor || "—"}</td>
    `;
    cuerpo.appendChild(tr);
  });

  document.getElementById("tabla-pie").textContent = `Mostrando ${visibles.length} de ${productos.length} productos`;
}

/* ---------- Nuevo producto ---------- */
function abrirModalNuevoProducto() {
  document.getElementById("input-nombre-producto").value = "";
  document.getElementById("input-sku-producto").value = "";
  document.getElementById("input-categoria-producto").value = "";
  document.getElementById("input-categoria-nueva").value = "";
  document.getElementById("input-costo-producto").value = "";
  document.getElementById("input-precio-producto").value = "";
  document.getElementById("input-stock-inicial").value = "";
  document.getElementById("input-stock-minimo").value = "";
  document.getElementById("input-proveedor-producto").value = "";
  document.getElementById("input-imagen-producto").value = "";
  imagenNuevoProducto = null;

  const placeholder = document.getElementById("imagen-placeholder");
  placeholder.innerHTML = `<span id="imagen-placeholder-icono">&#128444;</span><span id="imagen-placeholder-texto">Placeholder</span>`;

  document.querySelectorAll("#overlay-nuevo-producto .campo-error").forEach(el => el.classList.remove("campo-error"));
  document.querySelectorAll("#overlay-nuevo-producto .campo-mensaje-error").forEach(el => el.classList.remove("campo-mensaje-error--visible"));

  abrirModal("overlay-nuevo-producto");
}

function manejarSeleccionImagen(e) {
  const archivo = e.target.files[0];
  const placeholder = document.getElementById("imagen-placeholder");
  if (!archivo) return;

  if (archivo.size > LIMITE_IMAGEN_BYTES) {
    mostrarToast("La imagen es muy pesada para guardarla localmente (máx. ~180kb). Se usará un ícono por defecto.");
    imagenNuevoProducto = null;
    return;
  }

  const lector = new FileReader();
  lector.onload = () => {
    imagenNuevoProducto = lector.result;
    placeholder.innerHTML = `<img src="${imagenNuevoProducto}" alt="Vista previa">`;
  };
  lector.readAsDataURL(archivo);
}

function marcarError(inputId, mostrar) {
  const input = document.getElementById(inputId);
  const mensaje = input.closest(".campo").querySelector(".campo-mensaje-error");
  input.classList.toggle("campo-error", mostrar);
  if (mensaje) mensaje.classList.toggle("campo-mensaje-error--visible", mostrar);
  return !mostrar;
}

function guardarNuevoProducto() {
  const nombre = document.getElementById("input-nombre-producto").value.trim();
  const skuInput = document.getElementById("input-sku-producto").value.trim();
  const categoriaSelect = document.getElementById("input-categoria-producto").value;
  const categoriaNueva = document.getElementById("input-categoria-nueva").value.trim();
  const categoria = categoriaNueva || categoriaSelect;
  const costo = document.getElementById("input-costo-producto").value;
  const precio = document.getElementById("input-precio-producto").value;
  const stockInicial = document.getElementById("input-stock-inicial").value;
  const stockMinimo = document.getElementById("input-stock-minimo").value;
  const proveedor = document.getElementById("input-proveedor-producto").value.trim();

  const productos = obtenerProductos();
  const skuDuplicado = skuInput && productos.some(p => p.id.toLowerCase() === skuInput.toLowerCase());

  let valido = true;
  valido = marcarError("input-nombre-producto", !nombre) && valido;
  valido = marcarError("input-sku-producto", !skuInput || skuDuplicado) && valido;
  if (skuDuplicado) {
    document.getElementById("input-sku-producto").closest(".campo").querySelector(".campo-mensaje-error").textContent =
      "Ese código ya existe. Usa uno distinto.";
  }
  valido = marcarError("input-categoria-producto", !categoria) && valido;
  valido = marcarError("input-costo-producto", costo === "" || Number(costo) < 0) && valido;
  valido = marcarError("input-precio-producto", precio === "" || Number(precio) < 0) && valido;

  if (!valido) return;

  const nuevoProducto = {
    id: skuInput,
    nombre,
    categoria,
    costo: Number(costo),
    precio: Number(precio),
    stock: stockInicial === "" ? 0 : Math.max(0, parseInt(stockInicial, 10)),
    stockMinimo: stockMinimo === "" ? 0 : Math.max(0, parseInt(stockMinimo, 10)),
    proveedor: proveedor || "Sin proveedor",
    imagen: imagenNuevoProducto || null
  };

  productos.push(nuevoProducto);
  guardarProductos(productos);

  cerrarModal("overlay-nuevo-producto");
  renderInventario();
  mostrarToast(`"${nombre}" fue agregado al inventario`);
}
