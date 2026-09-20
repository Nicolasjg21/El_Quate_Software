/* ==========================================================================
   COMPRAS — compras(idCompra, idProveedor, fecha, total)
             detalleCompras(idDetalleCompra, idCompra, idProducto, cantidad, precioCompra)
   Al guardar: se crea la compra, luego cada línea del detalle y, si el Kardex
   automático está activo, la entrada de inventario correspondiente.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;

  var compras = [], detalles = [], proveedores = [], productos = [], stock = new Map();
  var lineas = [], busqueda = "";

  App.pagina(async function () {
    document.getElementById("botonMostrarForm").addEventListener("click", abrirFormulario);
    document.getElementById("botonCancelar").addEventListener("click", cerrarFormulario);
    document.getElementById("botonAgregarLinea").addEventListener("click", agregarLinea);
    document.getElementById("botonGuardar").addEventListener("click", guardar);
    document.getElementById("busquedaCompra").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintar();
    }, 200));
    document.getElementById("idProductoCompra").addEventListener("change", function () {
      var id = Number(ui.valor("idProductoCompra"));
      var ultimo = detalles.filter(function (d) { return d.idProducto === id; }).sort(function (a, b) { return b.idDetalleCompra - a.idDetalleCompra; })[0];
      ui.poner("precioCompra", ultimo ? ultimo.precioCompra : "");
    });
    ui.alHacerClic("cuerpoLineas", "button[data-indice]", function (b) {
      lineas.splice(Number(b.dataset.indice), 1); pintarLineas();
    });
    ui.alHacerClic("cuerpoTabla", "button[data-accion='ver']", function (b) { verDetalle(Number(b.dataset.id)); });

    await cargar();
  });

  async function cargar() {
    var d = await App.datos.cargar(["compras", "detalleCompras", "proveedores", "productos", "kardex"]);
    compras = d.compras; detalles = d.detalleCompras; proveedores = d.proveedores; productos = d.productos;
    stock = App.datos.stockPorProducto(d.kardex);
    ui.html("idProveedor", ui.opciones(proveedores, "idProveedor", "nombreProveedor", null, "— Selecciona un proveedor —"));
    ui.html("idProductoCompra", ui.opciones(productos, "idProducto", "nombreProducto", null, "— Selecciona un producto —"));
    if (!proveedores.length) ui.aviso("No hay proveedores registrados. Créalos en Catálogos antes de registrar compras.", "advertencia");
    pintar();
  }

  function nombreProveedor(id) { var p = proveedores.filter(function (x) { return x.idProveedor === id; })[0]; return p ? p.nombreProveedor : "Proveedor #" + id; }
  function nombreProducto(id) { var p = productos.filter(function (x) { return x.idProducto === id; })[0]; return p ? p.nombreProducto : "Producto #" + id; }

  function pintar() {
    var visibles = compras.filter(function (c) {
      return !busqueda || nombreProveedor(c.idProveedor).toLowerCase().indexOf(busqueda) !== -1;
    }).sort(function (a, b) { return b.idCompra - a.idCompra; });

    var filas = visibles.map(function (c) {
      var suyos = detalles.filter(function (d) { return d.idCompra === c.idCompra; });
      var articulos = suyos.reduce(function (s, d) { return s + Number(d.cantidad); }, 0);
      return "<tr><td class='texto-suave'>" + c.idCompra + "</td>" +
        "<td>" + ui.esc(nombreProveedor(c.idProveedor)) + "</td>" +
        "<td class='texto-xs texto-suave'>" + ui.esc(ui.soloFecha(c.fecha)) + "</td>" +
        "<td class='derecha'>" + ui.num(articulos) + "</td>" +
        "<td class='derecha'><strong>" + ui.moneda(c.total) + "</strong></td>" +
        "<td class='centro'><button class='pequeno' data-accion='ver' data-id='" + c.idCompra + "'>Ver detalle</button></td></tr>";
    });
    ui.html("cuerpoTabla", ui.filasOVacio(filas, 6, compras.length ? "Ninguna compra coincide con la búsqueda." : "Aún no hay compras registradas. Usa «+ Nueva compra» para anotar la primera.", { icono: "🛒" }));
  }

  function verDetalle(idCompra) {
    var suyos = detalles.filter(function (d) { return d.idCompra === idCompra; });
    if (!suyos.length) { ui.aviso("Esta compra no tiene líneas de detalle registradas.", "advertencia"); return; }
    ui.aviso("Compra #" + idCompra + ": " + suyos.map(function (d) {
      return nombreProducto(d.idProducto) + " ×" + d.cantidad + " a " + ui.moneda(d.precioCompra);
    }).join(" · "), "info");
  }

  function abrirFormulario() {
    lineas = [];
    ui.mostrar("formCompra", true);
    var hoy = new Date();
    ui.poner("fechaCompra", hoy.getFullYear() + "-" + ("0" + (hoy.getMonth() + 1)).slice(-2) + "-" + ("0" + hoy.getDate()).slice(-2));
    pintarLineas();
    window.scrollTo(0, 0);
  }
  function cerrarFormulario() { ui.mostrar("formCompra", false); lineas = []; }

  function agregarLinea() {
    var r = ui.validar([
      { id: "idProductoCompra", etiqueta: "El producto", requerido: true, tipo: "entero", min: 1 },
      { id: "cantidadCompra", etiqueta: "La cantidad", requerido: true, tipo: "entero", min: 1 },
      { id: "precioCompra", etiqueta: "El precio de compra", requerido: true, tipo: "decimal", min: 0, max: cfg.REGLAS.PRECIO_MAX }
    ]);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var existente = lineas.filter(function (l) { return l.idProducto === r.valores.idProductoCompra; })[0];
    if (existente) existente.cantidad += r.valores.cantidadCompra;
    else lineas.push({ idProducto: r.valores.idProductoCompra, cantidad: r.valores.cantidadCompra, precioCompra: r.valores.precioCompra });

    ui.poner("cantidadCompra", 1); ui.poner("precioCompra", "");
    pintarLineas();
  }

  function totalLineas() {
    return ui.redondear(lineas.reduce(function (s, l) { return s + l.cantidad * l.precioCompra; }, 0));
  }

  function pintarLineas() {
    var filas = lineas.map(function (l, i) {
      return "<tr><td>" + ui.esc(nombreProducto(l.idProducto)) + "</td>" +
        "<td class='derecha'>" + ui.num(l.cantidad) + "</td>" +
        "<td class='derecha'>" + ui.moneda(l.precioCompra) + "</td>" +
        "<td class='derecha'>" + ui.moneda(l.cantidad * l.precioCompra) + "</td>" +
        "<td class='centro'><button class='pequeno rojo' data-indice='" + i + "'>Quitar</button></td></tr>";
    });
    ui.html("cuerpoLineas", ui.filasOVacio(filas, 5, "Agrega al menos una línea de detalle."));
    ui.texto("totalCompra", ui.moneda(totalLineas()));
  }

  async function guardar() {
    var r = ui.validar([
      { id: "idProveedor", etiqueta: "El proveedor", requerido: true, tipo: "entero", min: 1 },
      { id: "fechaCompra", etiqueta: "La fecha", requerido: true }
    ]);
    if (!r.ok) { ui.reportarValidacion(r); return; }
    if (!lineas.length) { ui.aviso("Agrega al menos un producto al detalle de la compra.", "error"); return; }

    await ui.conBoton(document.getElementById("botonGuardar"), async function () {
      try {
        var total = totalLineas();
        await App.api.entidad("compras").crear({
          idProveedor: r.valores.idProveedor,
          fecha: r.valores.fechaCompra + "T00:00:00",
          total: total
        });
        // PostCompra no devuelve el id: se relee y se toma la más reciente del proveedor.
        var todas = await App.api.entidad("compras").listar();
        var creada = todas.filter(function (c) { return c.idProveedor === r.valores.idProveedor; })
                          .sort(function (a, b) { return b.idCompra - a.idCompra; })[0];
        if (!creada) throw new Error("La compra se registró pero no se pudo recuperar su identificador.");

        for (var i = 0; i < lineas.length; i++) {
          var l = lineas[i];
          await App.api.entidad("detalleCompras").crear({
            idCompra: creada.idCompra, idProducto: l.idProducto,
            cantidad: l.cantidad, precioCompra: l.precioCompra
          });
          if (cfg.OPCIONES.KARDEX_AUTOMATICO) {
            var actual = stock.get(l.idProducto) || 0;
            var nuevo = await App.datos.moverStock({
              idProducto: l.idProducto, tipo: "ENTRADA", cantidad: l.cantidad,
              stockActual: actual, motivo: "Compra #" + creada.idCompra
            });
            stock.set(l.idProducto, nuevo);
          }
        }
        await App.api.auditar("compras", "INSERT", null, { idCompra: creada.idCompra, total: total });
        ui.aviso("Compra registrada correctamente con " + lineas.length + " línea(s) de detalle.", "exito");
        cerrarFormulario();
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
    });
  }
})(window.App = window.App || {});
