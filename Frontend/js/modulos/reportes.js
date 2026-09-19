/* ==========================================================================
   VENTAS Y ANALÍTICAS
   --------------------------------------------------------------------------
   Pensado para llenarse con el Controller de Comprobantes cuando exista un
   endpoint de resumen. Mientras tanto usa datos de ejemplo, siempre
   marcados como tales, para que la pantalla se vea completa.
   ========================================================================== */

var ingresosVsCostos = [
  { dia: "Lun", ingresos: 8200, costos: 3100 },
  { dia: "Mar", ingresos: 7400, costos: 2900 },
  { dia: "Mié", ingresos: 9600, costos: 3300 },
  { dia: "Jue", ingresos: 11200, costos: 3600 },
  { dia: "Vie", ingresos: 15800, costos: 4200 },
  { dia: "Sáb", ingresos: 13400, costos: 3900 },
  { dia: "Dom", ingresos: 10100, costos: 3400 }
];

var porCategoria = [
  { nombre: "Cervezas", valor: 31 }, { nombre: "Licores", valor: 27 },
  { nombre: "Bebidas", valor: 15 }, { nombre: "Snacks", valor: 12 }, { nombre: "Otros", valor: 15 }
];

var porMetodoPago = [
  { nombre: "Efectivo", valor: 42 }, { nombre: "Tarjeta", valor: 39 }, { nombre: "Digital", valor: 19 }
];

var topProductos = [
  { nombre: "Cerveza Club Colombia Dorada", valor: 5472000, porcentaje: 100 },
  { nombre: "Whisky Old Parr",              valor: 3912000, porcentaje: 71 },
  { nombre: "Cerveza Águila Original",      valor: 2691000, porcentaje: 49 },
  { nombre: "Mojito Clásico",               valor: 2340000, porcentaje: 43 }
];

$(document).ready(function () {
    protegerPagina();
    $("#botonSalir").click(cerrarSesion);

    $(".chip").click(function () { $(".chip").removeClass("activo"); $(this).addClass("activo"); });
    $("#botonExportar").click(function () {
        var enlace = document.createElement("a");
        enlace.href = "data:text/plain;charset=utf-8," + encodeURIComponent(JSON.stringify(ingresosVsCostos, null, 2));
        enlace.download = "reporte-ventas.json";
        enlace.click();
    });

    dibujarKpis();
    dibujarGraficos();
    dibujarTop();
});


function dibujarKpis() {
    var ingresos = ingresosVsCostos.reduce(function (s, d) { return s + d.ingresos; }, 0);
    var costos = ingresosVsCostos.reduce(function (s, d) { return s + d.costos; }, 0);
    var utilidad = ingresos - costos;
    var margen = Math.round((utilidad / ingresos) * 100);

    $("#tarjetasKpi").html(
        tarjeta("💲", "Ingresos Totales", "$ " + formatoMiles(ingresos), "+12% vs. período anterior", true) +
        tarjeta("🛒", "Ganancia Bruta", "$ " + formatoMiles(utilidad), "Margen " + margen + "%", true) +
        tarjeta("📈", "Ticket Promedio", "$ " + formatoMiles(Math.round(ingresos / 245)), "+5% vs. período anterior", true) +
        tarjeta("%", "Órdenes Completadas", "245", "+8% vs. período anterior", true)
    );
}

function tarjeta(icono, etiqueta, valor, tendencia, sube) {
    return '<div class="kpi-tarjeta">' +
        '<div class="fila-icono"><div class="etiqueta">' + etiqueta + '</div><div class="icono">' + icono + '</div></div>' +
        '<div class="valor">' + valor + '</div>' +
        '<div class="tendencia ' + (sube ? "sube" : "baja") + '">' + (sube ? "↑ " : "↓ ") + tendencia + '</div>' +
    '</div>';
}


function dibujarGraficos() {
    $("#graficoIngresos").html(
        graficoLinea(ingresosVsCostos.map(function (d) { return d.ingresos; }), ingresosVsCostos.map(function (d) { return d.dia; }))
    );
    $("#graficoCategorias").html(graficoDona(porCategoria));
    $("#graficoPagos").html(graficoBarras(porMetodoPago.map(function (d) { return d.valor; }), porMetodoPago.map(function (d) { return d.nombre; })));
}


function dibujarTop() {
    var html = topProductos.map(function (p) {
        return '<div style="margin-bottom:14px">' +
            '<div style="display:flex;justify-content:space-between;font-size:13px;margin-bottom:4px">' +
                '<span>' + p.nombre + '</span><strong>$ ' + formatoMiles(p.valor) + '</strong>' +
            '</div>' +
            '<div class="barra-progreso"><div style="width:' + p.porcentaje + '%"></div></div>' +
        '</div>';
    }).join("");
    $("#listaTop").html(html);
}

function formatoMiles(n) { return new Intl.NumberFormat("es-CO").format(n || 0); }
