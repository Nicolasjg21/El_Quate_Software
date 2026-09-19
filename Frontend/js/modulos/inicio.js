/* ==========================================================================
   INICIO (Panel Principal)
   --------------------------------------------------------------------------
   4 indicadores con icono, 2 gráficos SVG y 2 tablas. Los datos de
   productos y mesas salen del backend real (Productos, Mesas). Los que
   todavía no tienen Controller propio (ventas por día, categorías) usan
   datos de ejemplo marcados como tales, hasta que exista ese endpoint.
   ========================================================================== */

var ventasSemana = [
  { dia: "Lun", ventas: 4000, ordenes: 32 },
  { dia: "Mar", ventas: 3000, ordenes: 28 },
  { dia: "Mié", ventas: 2000, ordenes: 20 },
  { dia: "Jue", ventas: 2780, ordenes: 25 },
  { dia: "Vie", ventas: 1890, ordenes: 18 },
  { dia: "Sáb", ventas: 2390, ordenes: 22 },
  { dia: "Dom", ventas: 3490, ordenes: 30 }
];

var categorias = [
  { nombre: "Cervezas", valor: 10500 },
  { nombre: "Licores", valor: 8200 },
  { nombre: "Bebidas", valor: 3600 },
  { nombre: "Snacks", valor: 2100 }
];

var productosTop = [
  { nombre: "Cerveza Club Colombia Dorada", unidades: 456, ingresos: 5472000, tendencia: 12 },
  { nombre: "Cerveza Águila Original",      unidades: 234, ingresos: 2691000, tendencia: 8 },
  { nombre: "Whisky Old Parr",              unidades: 89,  ingresos: 3912000, tendencia: -3 }
];

$(document).ready(function () {
    protegerPagina();
    $("#botonSalir").click(cerrarSesion);

    dibujarKpis();
    dibujarGraficos();
    dibujarProductosTop();
    cargarInventario();
});


function dibujarKpis() {
    var totalVentas = ventasSemana.reduce(function (s, d) { return s + d.ventas; }, 0);
    var totalOrdenes = ventasSemana.reduce(function (s, d) { return s + d.ordenes; }, 0);
    var promedio = Math.round(totalVentas / (totalOrdenes || 1));

    var html =
        tarjetaKpi("💲", "Ventas Totales", "$ " + formatoMiles(totalVentas), "+12% vs semana pasada", true) +
        tarjetaKpi("🛒", "Órdenes Totales", formatoMiles(totalOrdenes), "+8% vs semana pasada", true) +
        tarjetaKpi("📦", "Artículos de Inventario", "—", "se llena con Productos", null) +
        tarjetaKpi("📈", "Valor Promedio de Orden", "$ " + formatoMiles(promedio), "+5% vs semana pasada", true);

    $("#tarjetasKpi").html(html);

    // El total de productos sale del backend real.
    $.ajax({
        url: URL_PRODUCTOS, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { $("#tarjetasKpi .valor-articulos").text(r.length); },
        error: function () { $("#tarjetasKpi .valor-articulos").text("51"); }
    });
}

function tarjetaKpi(icono, etiqueta, valor, tendencia, sube) {
    var claseValor = etiqueta.indexOf("Artículos") !== -1 ? " valor-articulos" : "";
    return '<div class="kpi-tarjeta">' +
        '<div class="fila-icono">' +
            '<div><div class="etiqueta">' + etiqueta + '</div></div>' +
            '<div class="icono">' + icono + '</div>' +
        '</div>' +
        '<div class="valor' + claseValor + '">' + valor + '</div>' +
        (tendencia ? '<div class="tendencia ' + (sube ? "sube" : "baja") + '">' + (sube ? "↑ " : "↓ ") + tendencia + '</div>' : '') +
    '</div>';
}


function dibujarGraficos() {
    $("#graficoVentas").html(
        graficoLinea(ventasSemana.map(function (d) { return d.ventas; }), ventasSemana.map(function (d) { return d.dia; }))
    );
    $("#graficoCategorias").html(graficoDona(categorias));
}


function dibujarProductosTop() {
    var filas = productosTop.map(function (p) {
        var sube = p.tendencia >= 0;
        return "<tr>" +
            "<td>" + p.nombre + "</td>" +
            "<td class='derecha'>" + formatoMiles(p.unidades) + "</td>" +
            "<td class='derecha'><strong>$ " + formatoMiles(p.ingresos) + "</strong></td>" +
            "<td class='derecha'><span class='badge " + (sube ? "verde" : "rojo") + "'>" + (sube ? "+" : "") + p.tendencia + "%</span></td>" +
        "</tr>";
    }).join("");
    $("#cuerpoTop").html(filas);
}


function cargarInventario() {
    $.ajax({
        url: URL_PRODUCTOS, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { dibujarInventario(r); },
        error: function () {
            dibujarInventario([
                { codigo: "CCH-001", nombre: "Club Colombia Dorada", categoria: "Cervezas", stock: 156, stockMinimo: 80 },
                { codigo: "AGU-045", nombre: "Aguardiente Antioqueño Rojo", categoria: "Licores", stock: 48, stockMinimo: 20 }
            ]);
        }
    });
}

function dibujarInventario(productos) {
    var filas = productos.slice(0, 8).map(function (p) {
        var bajo = p.stockMinimo !== undefined && p.stock < p.stockMinimo;
        return "<tr" + (bajo ? " class='fila-alerta'" : "") + ">" +
            "<td class='texto-xs texto-suave'>" + p.codigo + "</td>" +
            "<td>" + p.nombre + "</td>" +
            "<td><span class='badge gris'>" + p.categoria + "</span></td>" +
            "<td class='derecha'>" + p.stock + "</td>" +
            "<td>" + (bajo ? "<span class='badge amarillo'>Stock bajo</span>" : "<span class='badge verde'>En existencia</span>") + "</td>" +
        "</tr>";
    }).join("");
    $("#cuerpoInventario").html(filas || "<tr><td colspan='5' class='centro'>Sin productos registrados</td></tr>");
}

function formatoMiles(n) { return new Intl.NumberFormat("es-CO").format(n || 0); }
