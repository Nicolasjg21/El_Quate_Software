/* ==========================================================================
   HISTORIALES — comprobantes y compras (solo lectura), con filtro de fecha
   real y exportación a CSV de verdad.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui;

  var comprobantes = [], compras = [], detalles = [], cuentas = [], mesas = [], metodos = [], proveedores = [];
  var pestana = "comprobantes", busqueda = "", rango = "todos";

  App.pagina(async function () {
    document.getElementById("botonPestanaComprobantes").addEventListener("click", function () { cambiar("comprobantes"); });
    document.getElementById("botonPestanaCompras").addEventListener("click", function () { cambiar("compras"); });
    document.getElementById("busquedaHistorial").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintar();
    }, 200));
    ui.$$(".chip").forEach(function (chip) {
      chip.addEventListener("click", function () {
        ui.$$(".chip").forEach(function (c) { c.classList.remove("activo"); });
        chip.classList.add("activo");
        rango = chip.dataset.rango;
        pintar();
      });
    });
    document.getElementById("botonExportar").addEventListener("click", exportar);

    var d = await App.datos.cargar(["comprobantes", "compras", "detalleCompras", "cuentas", "mesas", "metodosPago", "proveedores"]);
    comprobantes = d.comprobantes; compras = d.compras; detalles = d.detalleCompras;
    cuentas = d.cuentas; mesas = d.mesas; metodos = d.metodosPago; proveedores = d.proveedores;
    pintar();
  });

  function cambiar(cual) {
    pestana = cual;
    var esComp = cual === "comprobantes";
    ui.mostrar("bloqueComprobantes", esComp);
    ui.mostrar("bloqueCompras", !esComp);
    document.getElementById("botonPestanaComprobantes").classList.toggle("activa", esComp);
    document.getElementById("botonPestanaCompras").classList.toggle("activa", !esComp);
  }

  function desdeDelRango() {
    if (rango === "todos") return null;
    var d = ui.inicioDelDia();
    if (rango === "semana") d.setDate(d.getDate() - 7);
    if (rango === "mes") d.setMonth(d.getMonth() - 1);
    return d;
  }
  function enRango(valor) {
    var limite = desdeDelRango();
    if (!limite) return true;
    var f = ui.parseFecha(valor);
    return f && f >= limite;
  }

  function mesaDeCuenta(idCuenta) {
    var c = cuentas.filter(function (x) { return x.idCuenta === idCuenta; })[0];
    if (!c) return "—";
    var m = mesas.filter(function (x) { return x.idMesa === c.idMesa; })[0];
    return m ? "Mesa " + m.numeroMesa : "Mesa #" + c.idMesa;
  }
  function nombreMetodo(id) { var m = metodos.filter(function (x) { return x.idMetodo === id; })[0]; return m ? m.nombreMetodo : "Método #" + id; }
  function nombreProveedor(id) { var p = proveedores.filter(function (x) { return x.idProveedor === id; })[0]; return p ? p.nombreProveedor : "Proveedor #" + id; }
  function colorMetodo(nombre) {
    var n = String(nombre).toLowerCase();
    if (n.indexOf("efectivo") !== -1) return "verde";
    if (n.indexOf("tarjeta") !== -1) return "azul";
    return "morado";
  }

  function comprobantesVisibles() {
    return comprobantes.filter(function (c) {
      if (!enRango(c.fecha)) return false;
      if (!busqueda) return true;
      return (String(c.idComprobante) + " " + mesaDeCuenta(c.idCuenta) + " " + nombreMetodo(c.idMetodo)).toLowerCase().indexOf(busqueda) !== -1;
    }).sort(function (a, b) { return b.idComprobante - a.idComprobante; });
  }
  function comprasVisibles() {
    return compras.filter(function (c) {
      if (!enRango(c.fecha)) return false;
      if (!busqueda) return true;
      return (String(c.idCompra) + " " + nombreProveedor(c.idProveedor)).toLowerCase().indexOf(busqueda) !== -1;
    }).sort(function (a, b) { return b.idCompra - a.idCompra; });
  }
  function articulosDe(idCompra) {
    return detalles.filter(function (d) { return d.idCompra === idCompra; })
                   .reduce(function (s, d) { return s + Number(d.cantidad); }, 0);
  }

  function pintar() {
    ui.html("cuerpoComprobantes", ui.filasOVacio(comprobantesVisibles().map(function (c) {
      return "<tr><td class='texto-suave'>" + c.idComprobante + "</td>" +
        "<td>" + ui.esc(mesaDeCuenta(c.idCuenta)) + "</td>" +
        "<td class='texto-xs texto-suave'>" + ui.esc(ui.fecha(c.fecha)) + "</td>" +
        "<td>" + ui.badge(nombreMetodo(c.idMetodo), colorMetodo(nombreMetodo(c.idMetodo))) + "</td>" +
        "<td class='derecha'><strong>" + ui.moneda(c.total) + "</strong></td></tr>";
    }), 5, "No hay comprobantes en ese período."));

    ui.html("cuerpoCompras", ui.filasOVacio(comprasVisibles().map(function (c) {
      return "<tr><td class='texto-suave'>" + c.idCompra + "</td>" +
        "<td>" + ui.esc(nombreProveedor(c.idProveedor)) + "</td>" +
        "<td class='texto-xs texto-suave'>" + ui.esc(ui.soloFecha(c.fecha)) + "</td>" +
        "<td class='derecha'>" + ui.num(articulosDe(c.idCompra)) + "</td>" +
        "<td class='derecha'><strong>" + ui.moneda(c.total) + "</strong></td></tr>";
    }), 5, "No hay compras en ese período."));
  }

  function exportar() {
    if (pestana === "comprobantes") {
      ui.descargarCsv("comprobantes.csv", [
        { titulo: "Comprobante", valor: "idComprobante" },
        { titulo: "Mesa", valor: function (c) { return mesaDeCuenta(c.idCuenta); } },
        { titulo: "Fecha", valor: function (c) { return ui.fecha(c.fecha); } },
        { titulo: "Método de pago", valor: function (c) { return nombreMetodo(c.idMetodo); } },
        { titulo: "Total", valor: "total" }
      ], comprobantesVisibles());
    } else {
      ui.descargarCsv("compras.csv", [
        { titulo: "Compra", valor: "idCompra" },
        { titulo: "Proveedor", valor: function (c) { return nombreProveedor(c.idProveedor); } },
        { titulo: "Fecha", valor: function (c) { return ui.soloFecha(c.fecha); } },
        { titulo: "Artículos", valor: function (c) { return articulosDe(c.idCompra); } },
        { titulo: "Total", valor: "total" }
      ], comprasVisibles());
    }
  }
})(window.App = window.App || {});
