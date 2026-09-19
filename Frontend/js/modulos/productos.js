/* ==========================================================================
   PRODUCTOS (Inventario)
   --------------------------------------------------------------------------
   GET, POST, PUT, DELETE contra el Controller Productos. Si el backend
   todavía no responde, se muestran datos de ejemplo (productosEjemplo)
   para que la pantalla se vea completa mientras conectas la API.
   ========================================================================== */

var URL_PRODUCTOS_ACTUAL = URL_PRODUCTOS; // se usa igual, queda por claridad
var productos = [];
var textoBusqueda = "";
var categoriaFiltro = "todas";

/* Datos de ejemplo — sólo se ven si el backend no responde. */
var productosEjemplo = [
  { id: 1, codigo: "CER-001", nombre: "Águila Original 330ml",  categoria: "Cervezas", precioCompra: 2400, precioVenta: 7500, stock: 45, stockMinimo: 15 },
  { id: 2, codigo: "CER-005", nombre: "Corona Extra 330ml",     categoria: "Cervezas", precioCompra: 3200, precioVenta: 11000, stock: 4,  stockMinimo: 15 },
  { id: 3, codigo: "AGU-003", nombre: "Aguardiente Antioqueño Rojo 750ml", categoria: "Aguardientes", precioCompra: 28000, precioVenta: 95000, stock: 10, stockMinimo: 8 },
  { id: 4, codigo: "WHI-002", nombre: "Old Parr 18 años 750ml", categoria: "Whiskies", precioCompra: 120000, precioVenta: 395000, stock: 3, stockMinimo: 5 },
  { id: 5, codigo: "SNK-001", nombre: "Maní Salado (Bolsa)",    categoria: "Snacks",   precioCompra: 1800, precioVenta: 5000, stock: 60, stockMinimo: 20 }
];

$(document).ready(function () {
    protegerPagina();

    $("#botonSalir").click(cerrarSesion);
    $("#botonMostrarForm").click(function () { $("#formProducto").removeClass("oculto"); });
    $("#botonCancelar").click(limpiarFormulario);
    $("#botonGuardar").click(guardarProducto);

    $("#busquedaProducto").on("input", function () {
        textoBusqueda = $(this).val().toLowerCase();
        dibujarTabla();
    });

    $("#filtroCategoria").change(function () {
        categoriaFiltro = $(this).val();
        dibujarTabla();
    });

    listarProductos();
});


/* ---------- GET ---------- */
function listarProductos() {
    $.ajax({
        url: URL_PRODUCTOS,
        type: "GET",
        contentType: "application/json; charset=utf-8",

        success: function (respuesta) {
            productos = respuesta;
            dibujarTodo();
        },

        error: function () {
            productos = productosEjemplo;
            dibujarTodo();
        }
    });
}


function dibujarTodo() {
    llenarFiltroCategorias();
    dibujarResumen();
    dibujarTabla();
}


/* ---------- Opciones del filtro de categoría ---------- */
function llenarFiltroCategorias() {
    var categorias = [];
    productos.forEach(function (p) {
        if (categorias.indexOf(p.categoria) === -1) categorias.push(p.categoria);
    });

    var opciones = '<option value="todas">Todas las categorías</option>';
    categorias.forEach(function (c) {
        opciones += '<option value="' + c + '">' + c + '</option>';
    });
    $("#filtroCategoria").html(opciones);
}


/* ---------- Resumen con color ---------- */
function dibujarResumen() {
    var valorTotal = productos.reduce(function (s, p) { return s + p.precioVenta * p.stock; }, 0);
    var stockBajo = productos.filter(function (p) { return p.stock < p.stockMinimo; }).length;
    var sinExistencias = productos.filter(function (p) { return p.stock === 0; }).length;

    $("#resumenInventario").html(
        '<div class="estado-caja naranja">' +
          '<div class="etiqueta">Valor total del inventario</div>' +
          '<div class="valor">$ ' + formatoMiles(valorTotal) + '</div>' +
        '</div>' +
        '<div class="estado-caja amarilla">' +
          '<div class="etiqueta">Artículos con stock bajo</div>' +
          '<div class="valor">' + stockBajo + '</div>' +
        '</div>' +
        '<div class="estado-caja roja">' +
          '<div class="etiqueta">Sin existencias</div>' +
          '<div class="valor">' + sinExistencias + '</div>' +
        '</div>'
    );
}


/* ---------- Tabla, con filas resaltadas si el stock es bajo ---------- */
function dibujarTabla() {
    var visibles = productos.filter(function (p) {
        var coincideTexto = !textoBusqueda ||
            p.nombre.toLowerCase().indexOf(textoBusqueda) !== -1 ||
            p.codigo.toLowerCase().indexOf(textoBusqueda) !== -1;
        var coincideCategoria = categoriaFiltro === "todas" || p.categoria === categoriaFiltro;
        return coincideTexto && coincideCategoria;
    });

    $("#conteoProductos").text("Mostrando " + visibles.length + " de " + productos.length + " productos");

    var filas = "";
    for (var i = 0; i < visibles.length; i++) {
        var p = visibles[i];
        var bajo = p.stock < p.stockMinimo;
        var iniciales = p.nombre.split(" ").slice(0, 2).map(function (s) { return s.charAt(0); }).join("").toUpperCase();

        filas += "<tr" + (bajo ? ' class="fila-alerta"' : "") + ">" +
            "<td><div class='miniatura'>" + iniciales + "</div></td>" +
            "<td>" + p.codigo + "</td>" +
            "<td>" + p.nombre + "</td>" +
            "<td><span class='badge gris'>" + p.categoria + "</span></td>" +
            "<td class='derecha'>$ " + formatoMiles(p.precioCompra) + "</td>" +
            "<td class='derecha'><strong>$ " + formatoMiles(p.precioVenta) + "</strong></td>" +
            "<td class='derecha" + (bajo ? " dato-alerta" : "") + "'>" + (bajo ? "⚠ " : "") + p.stock + "</td>" +
            "<td class='centro'>" +
                "<button class='pequeno' onclick='editarProducto(" + p.id + ")'>Editar</button> " +
                "<button class='pequeno rojo' onclick='eliminarProducto(" + p.id + ")'>Eliminar</button>" +
            "</td>" +
        "</tr>";
    }

    if (visibles.length === 0) {
        filas = "<tr><td colspan='8' class='centro'>No se encontraron productos</td></tr>";
    }

    $("#cuerpoTabla").html(filas);
}


/* ---------- Crear / editar ---------- */
function guardarProducto() {
    var id = $("#idProducto").val();

    var producto = {
        codigo: $("#codigo").val(),
        nombre: $("#nombre").val(),
        categoria: $("#categoria").val(),
        precioCompra: Number($("#precioCompra").val()),
        precioVenta: Number($("#precioVenta").val()),
        stock: Number($("#stock").val())
    };

    if (producto.codigo === "" || producto.nombre === "") {
        mostrarMensaje("El código y el nombre son obligatorios", "error");
        return;
    }

    if (id === "") {
        $.ajax({
            url: URL_PRODUCTOS, type: "POST", contentType: "application/json; charset=utf-8",
            data: JSON.stringify(producto),
            success: function () { mostrarMensaje("Producto creado correctamente", "exito"); limpiarFormulario(); listarProductos(); },
            error: function (xhr) { mostrarMensaje("No se pudo guardar. Error " + xhr.status, "error"); }
        });
    } else {
        producto.id = Number(id);
        $.ajax({
            url: URL_PRODUCTOS + "/" + id, type: "PUT", contentType: "application/json; charset=utf-8",
            data: JSON.stringify(producto),
            success: function () { mostrarMensaje("Producto actualizado correctamente", "exito"); limpiarFormulario(); listarProductos(); },
            error: function (xhr) { mostrarMensaje("No se pudo actualizar. Error " + xhr.status, "error"); }
        });
    }
}


function editarProducto(id) {
    $.ajax({
        url: URL_PRODUCTOS + "/" + id, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { llenarFormulario(r); },
        error: function () {
            // Sin backend: busca en los datos de ejemplo que ya están en pantalla.
            var p = productos.filter(function (x) { return x.id === id; })[0];
            if (p) llenarFormulario(p);
        }
    });
}

function llenarFormulario(p) {
    $("#formProducto").removeClass("oculto");
    $("#idProducto").val(p.id);
    $("#codigo").val(p.codigo);
    $("#nombre").val(p.nombre);
    $("#categoria").val(p.categoria);
    $("#precioCompra").val(p.precioCompra);
    $("#precioVenta").val(p.precioVenta);
    $("#stock").val(p.stock);
    $("#tituloFormulario").text("Editando: " + p.nombre);
    window.scrollTo(0, 0);
}


function eliminarProducto(id) {
    if (!confirm("¿Seguro que deseas eliminar este producto?")) return;

    $.ajax({
        url: URL_PRODUCTOS + "/" + id, type: "DELETE", contentType: "application/json; charset=utf-8",
        success: function () { mostrarMensaje("Producto eliminado", "exito"); listarProductos(); },
        error: function (xhr) { mostrarMensaje("No se pudo eliminar. Error " + xhr.status, "error"); }
    });
}


function limpiarFormulario() {
    $("#idProducto, #codigo, #nombre, #categoria, #precioCompra, #precioVenta, #stock").val("");
    $("#tituloFormulario").text("Nuevo producto");
    $("#formProducto").addClass("oculto");
}

function formatoMiles(n) { return new Intl.NumberFormat("es-CO").format(n || 0); }

function mostrarMensaje(texto, tipo) {
    $("#mensaje").text(texto);
    $("#mensaje").attr("class", "mensaje " + tipo);
}
