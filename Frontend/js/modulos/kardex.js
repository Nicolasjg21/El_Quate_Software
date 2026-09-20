/* ==========================================================================
   KARDEX — movimientos de inventario
   idMovimiento, idProducto, tipoMovimiento, cantidad, stockAnterior,
   stockNuevo, motivo, fecha, idUsuario
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;

  var movimientos = [], productos = [], usuarios = [], stock = new Map();
  var filtroProducto = "todos", filtroTipo = "todos";

  App.pagina(async function () {
    ui.html("tipoMovimiento", ui.opciones(
      [{ v: cfg.ESTADOS.KARDEX.ENTRADA }, { v: cfg.ESTADOS.KARDEX.SALIDA }], "v", "v", cfg.ESTADOS.KARDEX.ENTRADA));
    ui.html("filtroTipo", "<option value='todos'>Entradas y salidas</option>" +
      ui.opciones([{ v: cfg.ESTADOS.KARDEX.ENTRADA }, { v: cfg.ESTADOS.KARDEX.SALIDA }], "v", "v"));

    document.getElementById("botonMostrarForm").addEventListener("click", function () { ui.mostrar("formMovimiento", true); });
    document.getElementById("botonCancelar").addEventListener("click", function () { ui.mostrar("formMovimiento", false); });
    document.getElementById("botonGuardar").addEventListener("click", guardar);
    document.getElementById("botonExportar").addEventListener("click", exportar);
    document.getElementById("filtroProducto").addEventListener("change", function (e) { filtroProducto = e.target.value; pintar(); });
    document.getElementById("filtroTipo").addEventListener("change", function (e) { filtroTipo = e.target.value; pintar(); });

    await cargar();
  });

  async function cargar() {
    var d = await App.datos.cargar(["kardex", "productos", "usuarios"]);
    movimientos = d.kardex; productos = d.productos; usuarios = d.usuarios;
    stock = App.datos.stockPorProducto(movimientos);

    ui.html("filtroProducto", "<option value='todos'>Todos los productos</option>" +
      ui.opciones(productos, "idProducto", "nombreProducto", filtroProducto));
    ui.html("idProducto", ui.opciones(productos, "idProducto", function (p) {
      return p.nombreProducto + " (stock: " + (stock.get(p.idProducto) || 0) + ")";
    }, null, "— Selecciona un producto —"));
    pintar();
  }

  function nombreProducto(id) { var p = productos.filter(function (x) { return x.idProducto === id; })[0]; return p ? p.nombreProducto : "Producto #" + id; }
  function nombreUsuario(id) { var u = usuarios.filter(function (x) { return x.idUsuario === id; })[0]; return u ? ui.nombreCompleto(u) : "Usuario #" + id; }

  function visibles() {
    return movimientos.filter(function (m) {
      var p = filtroProducto === "todos" || String(m.idProducto) === String(filtroProducto);
      var t = filtroTipo === "todos" || cfg.igual(m.tipoMovimiento, filtroTipo);
      return p && t;
    }).sort(function (a, b) {
      var fa = ui.parseFecha(a.fecha), fb = ui.parseFecha(b.fecha);
      return (fb ? fb.getTime() : 0) - (fa ? fa.getTime() : 0) || b.idMovimiento - a.idMovimiento;
    });
  }

  function pintar() {
    var lista = visibles();
    var filas = lista.map(function (m) {
      var entrada = cfg.igual(m.tipoMovimiento, cfg.ESTADOS.KARDEX.ENTRADA);
      return "<tr><td class='texto-xs texto-suave'>" + ui.esc(ui.fecha(m.fecha)) + "</td>" +
        "<td>" + ui.esc(nombreProducto(m.idProducto)) + "</td>" +
        "<td>" + ui.badge(m.tipoMovimiento, entrada ? "verde" : "naranja") + "</td>" +
        "<td class='derecha'>" + (entrada ? "+" : "−") + ui.num(m.cantidad) + "</td>" +
        "<td class='derecha texto-suave'>" + ui.num(m.stockAnterior) + "</td>" +
        "<td class='derecha'><strong>" + ui.num(m.stockNuevo) + "</strong></td>" +
        "<td class='texto-xs'>" + ui.esc(m.motivo || "—") + "</td>" +
        "<td class='texto-xs texto-suave'>" + ui.esc(nombreUsuario(m.idUsuario)) + "</td></tr>";
    });
    ui.html("cuerpoTabla", ui.filasOVacio(filas, 8, movimientos.length ? "Ningún movimiento coincide con el filtro." : "Aún no hay movimientos de inventario. Se generan al registrar stock inicial, compras, ventas o ajustes.", { icono: "📒" }));
    ui.texto("conteoKardex", "Mostrando " + lista.length + " de " + movimientos.length + " movimientos.");
  }

  async function guardar() {
    var r = ui.validar([
      { id: "idProducto", etiqueta: "El producto", requerido: true, tipo: "entero", min: 1 },
      { id: "cantidad", etiqueta: "La cantidad", requerido: true, tipo: "entero", min: 1 },
      { id: "motivo", etiqueta: "El motivo", largoMax: cfg.REGLAS.MOTIVO_MAX }
    ]);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var esEntrada = cfg.igual(ui.valor("tipoMovimiento"), cfg.ESTADOS.KARDEX.ENTRADA);
    await ui.conBoton(document.getElementById("botonGuardar"), async function () {
      try {
        await App.datos.moverStock({
          idProducto: r.valores.idProducto,
          tipo: esEntrada ? "ENTRADA" : "SALIDA",
          cantidad: r.valores.cantidad,
          stockActual: stock.get(r.valores.idProducto) || 0,
          motivo: r.valores.motivo || "Ajuste manual"
        });
        await App.api.auditar("kardex", "INSERT", null, { idProducto: r.valores.idProducto, cantidad: r.valores.cantidad });
        ui.aviso("Movimiento registrado correctamente.", "exito");
        ui.poner("cantidad", ""); ui.poner("motivo", "");
        ui.mostrar("formMovimiento", false);
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
    });
  }

  function exportar() {
    ui.descargarCsv("kardex.csv", [
      { titulo: "Fecha", valor: function (m) { return ui.fecha(m.fecha); } },
      { titulo: "Producto", valor: function (m) { return nombreProducto(m.idProducto); } },
      { titulo: "Tipo", valor: "tipoMovimiento" },
      { titulo: "Cantidad", valor: "cantidad" },
      { titulo: "Stock anterior", valor: "stockAnterior" },
      { titulo: "Stock nuevo", valor: "stockNuevo" },
      { titulo: "Motivo", valor: function (m) { return m.motivo || ""; } },
      { titulo: "Usuario", valor: function (m) { return nombreUsuario(m.idUsuario); } }
    ], visibles());
  }
})(window.App = window.App || {});
