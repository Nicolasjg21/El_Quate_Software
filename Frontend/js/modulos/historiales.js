/* ==========================================================================
   HISTORIALES
   --------------------------------------------------------------------------
   Dos tablas de sólo lectura: Comprobantes (GET) y Compras (GET), con
   pestañas, búsqueda y filtro por fecha. Si el backend no responde, se ven
   datos de ejemplo para que la pantalla no quede vacía.
   ========================================================================== */

var pestanaActiva = "comprobantes";
var textoBusqueda = "";
var rangoFecha = "todos";

var comprobantes = [];
var compras = [];

var comprobantesEjemplo = [
  { id: "ORD-2024-5847", mesa: 3, mesero: "Carlos Ramírez", fecha: "2024-05-12 23:45", metodo: "Efectivo", total: 186000 },
  { id: "ORD-2024-5846", mesa: 7, mesero: "Laura Gómez",    fecha: "2024-05-12 23:20", metodo: "Tarjeta",  total: 152000 },
  { id: "ORD-2024-5845", mesa: 2, mesero: "Santiago Pérez", fecha: "2024-05-12 22:50", metodo: "Digital",  total: 95000 }
];
var comprasEjemplo = [
  { id: "CMP-2024-1847", proveedor: "Bavaria S.A.",  fecha: "2024-05-10", articulos: 84, costo: 1480000 },
  { id: "CMP-2024-1846", proveedor: "Diageo Colombia", fecha: "2024-05-08", articulos: 30, costo: 3200000 }
];

$(document).ready(function () {
    protegerPagina();
    $("#botonSalir").click(cerrarSesion);

    $("#botonPestanaComprobantes").click(function () { cambiarPestana("comprobantes"); });
    $("#botonPestanaCompras").click(function () { cambiarPestana("compras"); });

    $("#busquedaHistorial").on("input", function () { textoBusqueda = $(this).val().toLowerCase(); dibujarTodo(); });

    $(".chip").click(function () {
        rangoFecha = $(this).data("rango");
        $(".chip").removeClass("activo");
        $(this).addClass("activo");
        dibujarTodo();
    });

    $("#botonExportar").click(exportarCsv);

    listarComprobantes();
    listarCompras();
});


function cambiarPestana(cual) {
    pestanaActiva = cual;
    var esComprobantes = cual === "comprobantes";
    $("#bloqueComprobantes").toggleClass("oculto", !esComprobantes);
    $("#bloqueCompras").toggleClass("oculto", esComprobantes);
    $("#botonPestanaComprobantes").toggleClass("activa", esComprobantes);
    $("#botonPestanaCompras").toggleClass("activa", !esComprobantes);
}


function listarComprobantes() {
    $.ajax({
        url: URL_COMPROBANTES, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { comprobantes = r; dibujarTodo(); },
        error: function () { comprobantes = comprobantesEjemplo; dibujarTodo(); }
    });
}

function listarCompras() {
    $.ajax({
        url: URL_COMPRAS, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { compras = r; dibujarTodo(); },
        error: function () { compras = comprasEjemplo; dibujarTodo(); }
    });
}


function dibujarTodo() {
    dibujarComprobantes();
    dibujarCompras();
}


function insigniaMetodo(metodo) {
    var colores = { "Efectivo": "verde", "Tarjeta": "azul", "Digital": "morado" };
    return "<span class='badge " + (colores[metodo] || "gris") + "'>" + metodo + "</span>";
}


function dibujarComprobantes() {
    var visibles = comprobantes.filter(function (c) {
        return !textoBusqueda ||
            String(c.mesero).toLowerCase().indexOf(textoBusqueda) !== -1 ||
            String(c.id).toLowerCase().indexOf(textoBusqueda) !== -1;
    });

    var filas = visibles.map(function (c) {
        return "<tr>" +
            "<td>" + c.id + "</td><td>Mesa " + c.mesa + "</td><td>" + c.mesero + "</td>" +
            "<td class='texto-xs texto-suave'>" + c.fecha + "</td>" +
            "<td>" + insigniaMetodo(c.metodo) + "</td>" +
            "<td class='derecha'><strong>$ " + formatoMiles(c.total) + "</strong></td>" +
        "</tr>";
    }).join("");

    $("#cuerpoComprobantes").html(filas || "<tr><td colspan='6' class='centro'>No hay órdenes que coincidan</td></tr>");
}


function dibujarCompras() {
    var visibles = compras.filter(function (c) {
        return !textoBusqueda ||
            String(c.proveedor).toLowerCase().indexOf(textoBusqueda) !== -1 ||
            String(c.id).toLowerCase().indexOf(textoBusqueda) !== -1;
    });

    var filas = visibles.map(function (c) {
        return "<tr>" +
            "<td>" + c.id + "</td><td>" + c.proveedor + "</td>" +
            "<td class='texto-xs texto-suave'>" + c.fecha + "</td>" +
            "<td class='derecha'>" + c.articulos + "</td>" +
            "<td class='derecha'><strong>$ " + formatoMiles(c.costo) + "</strong></td>" +
        "</tr>";
    }).join("");

    $("#cuerpoCompras").html(filas || "<tr><td colspan='5' class='centro'>No hay compras que coincidan</td></tr>");
}


function exportarCsv() {
    var datos = pestanaActiva === "comprobantes" ? comprobantes : compras;
    var csv = JSON.stringify(datos, null, 2);
    var enlace = document.createElement("a");
    enlace.href = "data:text/plain;charset=utf-8," + encodeURIComponent(csv);
    enlace.download = pestanaActiva + ".json";
    enlace.click();
}

function formatoMiles(n) { return new Intl.NumberFormat("es-CO").format(n || 0); }
