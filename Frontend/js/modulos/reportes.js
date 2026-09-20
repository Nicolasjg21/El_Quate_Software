/* ==========================================================================
   REPORTES — ingresos (comprobantes) vs costos (compras), categorías,
   métodos de pago y productos más vendidos. Todo con datos reales.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui;

  var datos = null, rango = "semana";

  App.pagina(async function () {
    ui.$$(".chip").forEach(function (chip) {
      chip.addEventListener("click", function () {
        ui.$$(".chip").forEach(function (c) { c.classList.remove("activo"); });
        chip.classList.add("activo");
        rango = chip.dataset.rango;
        pintar();
      });
    });
    document.getElementById("botonExportar").addEventListener("click", exportar);

    datos = await App.datos.cargar(["comprobantes", "compras", "cuentas", "pedidos", "detallePedidos", "productos", "categorias", "metodosPago"]);
    pintar();
  });

  function limite() {
    if (rango === "todos") return null;
    var d = ui.inicioDelDia();
    if (rango === "semana") d.setDate(d.getDate() - 6);
    if (rango === "mes") d.setMonth(d.getMonth() - 1);
    return d;
  }
  function dentro(valor) {
    var l = limite();
    if (!l) return true;
    var f = ui.parseFecha(valor);
    return f && f >= l;
  }

  function pintar() {
    var comprobantes = datos.comprobantes.filter(function (c) { return dentro(c.fecha); });
    var compras = datos.compras.filter(function (c) { return dentro(c.fecha); });

    var ingresos = comprobantes.reduce(function (s, c) { return s + Number(c.total); }, 0);
    var costos = compras.reduce(function (s, c) { return s + Number(c.total); }, 0);
    var utilidad = ingresos - costos;
    var margen = ingresos ? Math.round((utilidad / ingresos) * 100) : 0;

    ui.html("tarjetasKpi",
      kpi("💲", "Ingresos cobrados", ui.moneda(ingresos)) +
      kpi("📦", "Costo de compras", ui.moneda(costos)) +
      kpi("📈", "Diferencia", ui.moneda(utilidad), margen + "% sobre ingresos") +
      kpi("🧾", "Comprobantes", ui.num(comprobantes.length)));

    /* Serie diaria */
    var dias = rango === "mes" ? 30 : (rango === "hoy" ? 1 : 7);
    if (rango === "todos") dias = 14;
    var etiquetas = [], serieIngresos = [], serieCostos = [];
    for (var i = dias - 1; i >= 0; i--) {
      var d0 = ui.inicioDelDia(); d0.setDate(d0.getDate() - i);
      var d1 = new Date(d0.getTime() + 86400000);
      etiquetas.push(("0" + d0.getDate()).slice(-2) + "/" + ("0" + (d0.getMonth() + 1)).slice(-2));
      serieIngresos.push(sumarEntre(datos.comprobantes, d0, d1));
      serieCostos.push(sumarEntre(datos.compras, d0, d1));
    }
    ui.html("graficoIngresos", App.graficos.linea([
      { nombre: "Ingresos", valores: serieIngresos },
      { nombre: "Costos de compra", valores: serieCostos }
    ], etiquetas));

    /* Categorías (a partir del detalle de las cuentas cobradas) */
    var cobradas = new Set(comprobantes.map(function (c) { return c.idCuenta; }));
    var porCuenta = App.datos.detallesPorCuenta(datos.pedidos, datos.detallePedidos);
    var vendidos = [];
    porCuenta.forEach(function (lista, id) { if (cobradas.has(id)) vendidos = vendidos.concat(lista); });

    var prodPorId = ui.indexar(datos.productos, "idProducto");
    var porCat = new Map(), porProd = new Map();
    vendidos.forEach(function (x) {
      var p = prodPorId.get(x.idProducto);
      var importe = Number(x.cantidad) * Number(x.precioUnitario);
      if (p) porCat.set(p.idCategoria, (porCat.get(p.idCategoria) || 0) + importe);
      porProd.set(x.idProducto, (porProd.get(x.idProducto) || 0) + importe);
    });

    ui.html("graficoCategorias", App.graficos.dona(datos.categorias.map(function (c) {
      return { nombre: c.nombreCategoria, valor: porCat.get(c.idCategoria) || 0 };
    }).filter(function (x) { return x.valor > 0; })));

    /* Métodos de pago */
    var porMetodo = datos.metodosPago.map(function (m) {
      return { nombre: m.nombreMetodo, valor: comprobantes.filter(function (c) { return c.idMetodo === m.idMetodo; })
        .reduce(function (s, c) { return s + Number(c.total); }, 0) };
    }).filter(function (x) { return x.valor > 0; });
    ui.html("graficoPagos", App.graficos.barras(porMetodo.map(function (x) { return x.valor; }), porMetodo.map(function (x) { return x.nombre; })));

    /* Top de productos */
    var top = Array.from(porProd.entries()).map(function (e) {
      var p = prodPorId.get(e[0]);
      return { nombre: p ? p.nombreProducto : "Producto #" + e[0], valor: e[1] };
    }).sort(function (a, b) { return b.valor - a.valor; }).slice(0, 6);
    var maximo = top.length ? top[0].valor : 1;

    ui.html("listaTop", top.length ? top.map(function (p) {
      return "<div style='margin-bottom:14px'><div style='display:flex;justify-content:space-between;font-size:13px;margin-bottom:4px'>" +
        "<span>" + ui.esc(p.nombre) + "</span><strong>" + ui.moneda(p.valor) + "</strong></div>" +
        "<div class='barra-progreso'><div style='width:" + Math.round((p.valor / maximo) * 100) + "%'></div></div></div>";
    }).join("") : "<p class='texto-suave'>Todavía no hay ventas cobradas en este período.</p>");
  }

  function sumarEntre(lista, d0, d1) {
    return lista.reduce(function (s, x) {
      var f = ui.parseFecha(x.fecha);
      return (f && f >= d0 && f < d1) ? s + Number(x.total) : s;
    }, 0);
  }

  function kpi(icono, etiqueta, valor, extra) {
    return "<div class='kpi-tarjeta'><div class='fila-icono'><div><div class='etiqueta'>" + ui.esc(etiqueta) +
      "</div></div><div class='icono'>" + icono + "</div></div><div class='valor'>" + ui.esc(valor) + "</div>" +
      (extra ? "<div class='tendencia sube'>" + ui.esc(extra) + "</div>" : "") + "</div>";
  }

  function exportar() {
    ui.descargarCsv("reporte-comprobantes.csv", [
      { titulo: "Comprobante", valor: "idComprobante" },
      { titulo: "Cuenta", valor: "idCuenta" },
      { titulo: "Fecha", valor: function (c) { return ui.fecha(c.fecha); } },
      { titulo: "Total", valor: "total" }
    ], datos.comprobantes.filter(function (c) { return dentro(c.fecha); }));
  }
})(window.App = window.App || {});
