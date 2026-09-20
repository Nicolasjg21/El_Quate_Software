/* ==========================================================================
   INICIO — todos los indicadores salen de la base de datos.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;

  App.pagina(async function () {
    var d = await App.datos.cargar(["comprobantes", "cuentas", "pedidos", "detallePedidos", "productos",
                                    "categorias", "kardex", "mesas", "metodosPago", "proveedores"]);
    var yo = App.sesion.datos() || {};

    saludo(yo);
    primerosPasos(d);

    var stock = App.datos.stockPorProducto(d.kardex);
    var porCuenta = App.datos.detallesPorCuenta(d.pedidos, d.detallePedidos);

    /* ---- KPIs: últimos 7 días contra los 7 anteriores ---- */
    var hoy0 = ui.inicioDelDia();
    var desde = new Date(hoy0); desde.setDate(desde.getDate() - 6);
    var desdePrev = new Date(desde); desdePrev.setDate(desdePrev.getDate() - 7);

    function entre(lista, a, b) {
      return lista.filter(function (c) { var f = ui.parseFecha(c.fecha); return f && f >= a && f < b; });
    }
    var fin = new Date(hoy0.getTime() + 86400000);
    var actual = entre(d.comprobantes, desde, fin);
    var previo = entre(d.comprobantes, desdePrev, desde);
    var suma = function (l) { return l.reduce(function (s, c) { return s + Number(c.total); }, 0); };
    var ingresos = suma(actual), ingresosPrev = suma(previo);
    var ticket = actual.length ? ingresos / actual.length : 0;
    var ticketPrev = previo.length ? ingresosPrev / previo.length : 0;
    var ocupadas = d.mesas.filter(function (m) { return cfg.igual(m.estado, cfg.ESTADOS.MESA.OCUPADA); }).length;

    ui.html("tarjetasKpi",
      kpi("💲", "Ventas (7 días)", ui.moneda(ingresos), tendencia(ingresos, ingresosPrev)) +
      kpi("🧾", "Comprobantes (7 días)", ui.num(actual.length), tendencia(actual.length, previo.length)) +
      kpi("📈", "Ticket promedio", ui.moneda(ticket), tendencia(ticket, ticketPrev)) +
      kpi("🍽️", "Mesas ocupadas", ocupadas + " / " + d.mesas.length,
          { texto: d.mesas.length ? Math.round((ocupadas / d.mesas.length) * 100) + "% del salón" : "Sin mesas registradas", clase: "neutra" }));

    /* ---- Ventas por día ---- */
    var etiquetas = [], valores = [];
    for (var i = 6; i >= 0; i--) {
      var dia = new Date(hoy0); dia.setDate(dia.getDate() - i);
      var sig = new Date(dia.getTime() + 86400000);
      etiquetas.push(dia.toLocaleDateString("es-CO", { weekday: "short" }));
      valores.push(suma(entre(d.comprobantes, dia, sig)));
    }
    ui.html("graficoVentas", App.graficos.linea([{ nombre: "Ventas", valores: valores }], etiquetas));

    /* ---- Categorías y top de productos (solo cuentas cobradas) ---- */
    var cobradas = new Set(d.comprobantes.map(function (c) { return c.idCuenta; }));
    var vendidos = [];
    porCuenta.forEach(function (lista, idCuenta) { if (cobradas.has(idCuenta)) vendidos = vendidos.concat(lista); });

    var prodPorId = ui.indexar(d.productos, "idProducto");
    var porCategoria = new Map(), porProducto = new Map();
    vendidos.forEach(function (x) {
      var p = prodPorId.get(x.idProducto);
      var importe = Number(x.cantidad) * Number(x.precioUnitario);
      if (p) porCategoria.set(p.idCategoria, (porCategoria.get(p.idCategoria) || 0) + importe);
      var acc = porProducto.get(x.idProducto) || { unidades: 0, ingresos: 0 };
      acc.unidades += Number(x.cantidad); acc.ingresos += importe;
      porProducto.set(x.idProducto, acc);
    });

    ui.html("graficoCategorias", App.graficos.dona(d.categorias.map(function (c) {
      return { nombre: c.nombreCategoria, valor: porCategoria.get(c.idCategoria) || 0 };
    }).filter(function (x) { return x.valor > 0; })));

    var top = Array.from(porProducto.entries())
      .map(function (e) { var p = prodPorId.get(e[0]); return { nombre: p ? p.nombreProducto : "Producto #" + e[0], unidades: e[1].unidades, ingresos: e[1].ingresos }; })
      .sort(function (a, b) { return b.ingresos - a.ingresos; }).slice(0, 5);

    ui.html("cuerpoTop", ui.filasOVacio(top.map(function (t) {
      return "<tr><td>" + ui.esc(t.nombre) + "</td><td class='derecha'>" + ui.num(t.unidades) +
             "</td><td class='derecha'><strong>" + ui.moneda(t.ingresos) + "</strong></td></tr>";
    }), 3, "Aún no hay ventas cobradas.", { icono: "🏆" }));

    /* ---- Cuentas abiertas ahora ---- */
    var abiertas = d.cuentas.filter(function (c) { return cfg.igual(c.estado, cfg.ESTADOS.CUENTA.ABIERTA); })
      .sort(function (a, b) { return b.idCuenta - a.idCuenta; });
    var mesaPorId = ui.indexar(d.mesas, "idMesa");
    ui.html("listaCuentas", abiertas.length ? abiertas.slice(0, 6).map(function (c) {
      var m = mesaPorId.get(c.idMesa);
      return "<a class='cuenta-fila' href='cuenta.html?idMesa=" + c.idMesa + "'>" +
        "<div><strong>Mesa " + ui.esc(m ? m.numeroMesa : c.idMesa) + "</strong><small>Abierta " + ui.esc(ui.hace(c.fechaApertura)) + "</small></div>" +
        "<strong>" + ui.moneda(App.datos.sumar(porCuenta.get(c.idCuenta))) + "</strong></a>";
    }).join("") : ui.vacio("🍽️", "No hay cuentas abiertas", "Cuando una mesa se ocupe, aparecerá aquí con su consumo actual.",
                           "<a class='boton-enlace' href='mesas.html'>Ir a las mesas</a>"));

    /* ---- Productos en o bajo el mínimo ---- */
    var catPorId = ui.indexar(d.categorias, "idCategoria");
    var bajos = d.productos.filter(function (p) { return p.estado && (stock.get(p.idProducto) || 0) <= Number(p.cantidadMinima); })
      .sort(function (a, b) { return (stock.get(a.idProducto) || 0) - (stock.get(b.idProducto) || 0); }).slice(0, 10);

    ui.html("cuerpoInventario", ui.filasOVacio(bajos.map(function (p) {
      var s = stock.get(p.idProducto) || 0, cat = catPorId.get(p.idCategoria);
      return "<tr class='fila-alerta'><td>" + ui.esc(p.nombreProducto) + "</td>" +
        "<td>" + ui.badge(cat ? cat.nombreCategoria : "—", "gris") + "</td>" +
        "<td class='derecha dato-alerta'>" + ui.num(s) + "</td><td class='derecha'>" + ui.num(p.cantidadMinima) + "</td>" +
        "<td>" + ui.badge(s <= 0 ? "Sin existencias" : "Stock bajo", s <= 0 ? "rojo" : "amarillo") + "</td></tr>";
    }), 5, d.productos.length ? "Todos los productos tienen existencias suficientes. 👍" : "Todavía no hay productos registrados.",
       { icono: d.productos.length ? "✅" : "📦", accion: d.productos.length ? "" : "<a class='boton-enlace' href='productos.html'>Registrar productos</a>" }));
  });

  function saludo(yo) {
    var h = new Date().getHours();
    var momento = h < 12 ? "Buenos días" : (h < 19 ? "Buenas tardes" : "Buenas noches");
    var nombre = String(yo.nombre || "").split(" ")[0];
    ui.texto("saludoTexto", momento + (nombre ? ", " + nombre : "") + " 👋");
    ui.texto("saludoSub", new Date().toLocaleDateString("es-CO", { weekday: "long", day: "numeric", month: "long", year: "numeric" }));
  }

  /* Guía para una base recién creada: qué falta registrar y dónde. */
  function primerosPasos(d) {
    var pasos = [
      { ok: d.categorias.length, t: "Crear categorías", s: "Agrupan tus productos (cervezas, snacks…)", href: "catalogos.html?c=categorias" },
      { ok: d.productos.length, t: "Registrar productos", s: "Con precio de venta, mínimo y stock inicial", href: "productos.html" },
      { ok: d.mesas.length, t: "Registrar mesas", s: "Las del salón, para poder abrir cuentas", href: "mesas.html" },
      { ok: d.metodosPago.length, t: "Definir métodos de pago", s: "Efectivo, tarjeta, transferencia…", href: "catalogos.html?c=metodosPago" },
      { ok: d.proveedores.length, t: "Registrar proveedores", s: "Para poder anotar tus compras", href: "catalogos.html?c=proveedores" }
    ];
    var hechos = pasos.filter(function (p) { return p.ok; }).length;
    if (hechos === pasos.length) { ui.html("bloquePrimerosPasos", ""); return; }
    ui.html("bloquePrimerosPasos",
      "<div class='tarjeta primeros-pasos'><h3>🚀 Primeros pasos <span class='texto-suave' style='font-weight:normal;font-size:13px'>(" + hechos + " de " + pasos.length + ")</span></h3>" +
      "<p class='texto-suave' style='margin-top:4px'>Para que el sistema empiece a mostrar información, registra estos datos base:</p>" +
      "<ul class='pasos'>" + pasos.map(function (p) {
        return "<li class='" + (p.ok ? "hecho" : "") + "'><span class='marca'>" + (p.ok ? "✓" : "") + "</span>" +
          "<span class='texto'>" + ui.esc(p.t) + "<small>" + ui.esc(p.s) + "</small></span>" +
          (p.ok ? "" : "<a href='" + p.href + "'>Hacerlo →</a>") + "</li>";
      }).join("") + "</ul></div>");
  }

  function tendencia(actual, previo) {
    if (!previo) return { texto: actual ? "Sin período anterior para comparar" : "Sin movimientos aún", clase: "neutra" };
    var pct = Math.round(((actual - previo) / previo) * 100);
    if (pct === 0) return { texto: "Igual que la semana anterior", clase: "neutra" };
    return { texto: (pct > 0 ? "▲ " : "▼ ") + Math.abs(pct) + "% vs. semana anterior", clase: pct > 0 ? "sube" : "baja" };
  }

  function kpi(icono, etiqueta, valor, t) {
    return "<div class='kpi-tarjeta'><div class='fila-icono'><div><div class='etiqueta'>" + ui.esc(etiqueta) +
      "</div></div><div class='icono'>" + icono + "</div></div><div class='valor'>" + ui.esc(valor) + "</div>" +
      "<div class='tendencia " + t.clase + "'>" + ui.esc(t.texto) + "</div></div>";
  }
})(window.App = window.App || {});
