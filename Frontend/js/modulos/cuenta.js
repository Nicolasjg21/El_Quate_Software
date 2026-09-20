/* ==========================================================================
   CUENTA DE MESA (punto de venta)
   --------------------------------------------------------------------------
   Cadena real del modelo (no se puede saltar ningún eslabón):
     mesas → cuentas(idMesa, estado, fechaApertura, total)
           → pedidos(idCuenta, idUsuario, fecha, estadoPedido)
           → detallePedidos(idPedido, idProducto, cantidad, precioUnitario)
           → comprobantes(idCuenta, fecha, total, idMetodo)
   El detalle NO tiene idMesa: se cuelga de un pedido, y el pedido de una cuenta.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;

  var idMesa = null, mesa = null, cuenta = null, pedido = null;
  var productos = [], categorias = [], metodos = [], detalles = [], stock = new Map();
  var filtroCat = "todas", busqueda = "";
  var cola = Promise.resolve();          // serializa las operaciones para que toques rápidos no se pisen

  App.pagina(async function () {
    idMesa = Number(new URLSearchParams(window.location.search).get("idMesa"));
    if (!idMesa) {
      ui.html("bloqueCuenta", ui.vacio("🍽️", "No se indicó qué mesa abrir", "Vuelve a Mesas y selecciona una.",
        "<a class='boton-enlace' href='mesas.html'>Ir a las mesas</a>"));
      return;
    }

    document.getElementById("botonAbrirCuenta").addEventListener("click", abrirCuenta);
    document.getElementById("botonPagar").addEventListener("click", pagar);
    document.getElementById("botonCancelarCuenta").addEventListener("click", cancelarCuenta);
    document.getElementById("busquedaProducto").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintarCatalogo();
    }, 150));

    ui.alHacerClic("productosRejilla", "button[data-producto]", function (b) { encolar(function () { agregar(Number(b.dataset.producto)); }); });
    ui.alHacerClic("chipsCategorias", "button[data-cat]", function (b) {
      filtroCat = b.dataset.cat; pintarChips(); pintarCatalogo();
    });
    ui.alHacerClic("ticketLineas", "button[data-accion]", function (b) {
      var id = Number(b.dataset.id);
      encolar(function () { return cambiarCantidad(id, b.dataset.accion); });
    });

    await cargar();
  });

  /* Ejecuta una operación tras las anteriores y muestra el error si falla. */
  function encolar(tarea) {
    cola = cola.then(tarea).catch(function (e) { ui.aviso(App.api.explicar(e), "error"); });
    return cola;
  }

  async function cargar() {
    var d = await App.datos.cargar(["mesas", "cuentas", "pedidos", "detallePedidos", "productos", "categorias", "metodosPago", "kardex"]);
    productos = d.productos.filter(function (p) { return p.estado; });
    categorias = d.categorias; metodos = d.metodosPago;
    stock = App.datos.stockPorProducto(d.kardex);

    mesa = d.mesas.filter(function (m) { return m.idMesa === idMesa; })[0];
    if (!mesa) {
      ui.html("bloqueCuenta", ui.vacio("❓", "La mesa #" + idMesa + " no existe", "", "<a class='boton-enlace' href='mesas.html'>Ir a las mesas</a>"));
      return;
    }

    document.body.dataset.titulo = "Mesa " + mesa.numeroMesa;
    var h1 = document.querySelector(".encabezado h1"); if (h1) h1.textContent = "Mesa " + mesa.numeroMesa;
    ui.texto("tituloCuenta", "Mesa " + mesa.numeroMesa);

    cuenta = App.datos.cuentaAbierta(d.cuentas, idMesa);
    if (!cuenta) {
      ui.html("datosCuenta", "");
      ui.mostrar("sinCuenta", true);
      ui.mostrar("bloquePos", false);
      return;
    }
    ui.mostrar("sinCuenta", false);
    ui.mostrar("bloquePos", true);

    // Pedido vigente (el más reciente); si no hay, se crea al agregar el primer producto.
    var suyos = d.pedidos.filter(function (p) { return p.idCuenta === cuenta.idCuenta; })
                         .sort(function (a, b) { return b.idPedido - a.idPedido; });
    pedido = suyos[0] || null;
    var ids = suyos.map(function (p) { return p.idPedido; });
    detalles = d.detallePedidos.filter(function (x) { return ids.indexOf(x.idPedido) !== -1; });

    ui.html("datosCuenta",
      "<span>Cuenta <strong>#" + cuenta.idCuenta + "</strong></span>" +
      "<span>Estado <strong>" + ui.esc(cuenta.estado) + "</strong></span>" +
      "<span>Abierta <strong>" + ui.esc(ui.fecha(cuenta.fechaApertura)) + " (" + ui.esc(ui.hace(cuenta.fechaApertura)) + ")</strong></span>");

    ui.html("metodoPago", ui.opciones(metodos, "idMetodo", "nombreMetodo", null, "— Selecciona el método —"));
    if (!metodos.length) ui.aviso("No hay métodos de pago registrados. Crea al menos uno en Catálogos para poder cobrar.", "advertencia");
    if (!productos.length) ui.aviso("No hay productos activos para vender. Regístralos en Inventario.", "advertencia");

    pintarChips();
    pintarCatalogo();
    pintarTicket();
  }

  /* ---------- Catálogo ---------- */
  function pintarChips() {
    var chips = "<button class='chip" + (filtroCat === "todas" ? " activo" : "") + "' data-cat='todas'>Todas</button>" +
      categorias.map(function (c) {
        return "<button class='chip" + (String(filtroCat) === String(c.idCategoria) ? " activo" : "") + "' data-cat='" + c.idCategoria + "'>" + ui.esc(c.nombreCategoria) + "</button>";
      }).join("");
    ui.html("chipsCategorias", chips);
  }

  function pintarCatalogo() {
    var visibles = productos.filter(function (p) {
      var c = filtroCat === "todas" || String(p.idCategoria) === String(filtroCat);
      var t = !busqueda || String(p.nombreProducto).toLowerCase().indexOf(busqueda) !== -1;
      return c && t;
    });
    ui.html("productosRejilla", visibles.length ? visibles.map(function (p) {
      var s = stock.get(p.idProducto) || 0, bajo = s <= Number(p.cantidadMinima);
      return "<button class='producto-tile' data-producto='" + p.idProducto + "'>" +
        "<span class='nombre'>" + ui.esc(p.nombreProducto) + "</span>" +
        "<span class='stock" + (bajo ? " bajo" : "") + "'>" + (s <= 0 ? "Sin stock" : "Stock: " + ui.num(s)) + "</span>" +
        "<span class='precio'>" + ui.moneda(p.precioVenta) + "</span></button>";
    }).join("") : "<div style='grid-column:1/-1'>" + ui.vacio("🔍", "No hay productos para mostrar", "Cambia la categoría o la búsqueda.") + "</div>");
  }

  /* ---------- Ticket ---------- */
  function totalActual() { return App.datos.sumar(detalles); }

  function pintarTicket() {
    var total = totalActual();
    ui.html("ticketLineas", detalles.length ? detalles.map(function (d) {
      var p = productos.filter(function (x) { return x.idProducto === d.idProducto; })[0];
      var importe = Number(d.cantidad) * Number(d.precioUnitario);
      var unico = Number(d.cantidad) <= 1;
      return "<div class='linea-ticket'>" +
        "<div><div class='nombre'>" + ui.esc(p ? p.nombreProducto : "Producto #" + d.idProducto) + "</div>" +
        "<div class='unit'>" + ui.moneda(d.precioUnitario) + " c/u</div></div>" +
        "<div class='importe'>" + ui.moneda(importe) + "</div>" +
        "<div class='cantidad'>" +
          "<button data-accion='menos' data-id='" + d.idDetalle + "' class='" + (unico ? "quitar" : "") + "' title='" + (unico ? "Quitar" : "Restar uno") + "'>" + (unico ? "🗑" : "−") + "</button>" +
          "<span>" + ui.num(d.cantidad) + "</span>" +
          "<button data-accion='mas' data-id='" + d.idDetalle + "' title='Sumar uno'>+</button>" +
        "</div></div>";
    }).join("") : "<div class='vacio-ticket'>🧾<br>Aún no hay productos.<br><small>Toca uno del catálogo para agregarlo.</small></div>");

    ui.texto("subtotalTexto", ui.moneda(total));
    ui.texto("totalTexto", ui.moneda(total));
    ui.texto("etiquetaPedido", pedido ? "Pedido #" + pedido.idPedido : "Sin pedido");
    document.getElementById("botonPagar").disabled = detalles.length === 0;
    ui.mostrar("botonCancelarCuenta", detalles.length === 0);
  }

  async function refrescarDetalles() {
    var todos = await App.api.entidad("detallePedidos").listar();
    var pedidos = await App.api.entidad("pedidos").listar();
    var ids = pedidos.filter(function (p) { return p.idCuenta === cuenta.idCuenta; }).map(function (p) { return p.idPedido; });
    detalles = todos.filter(function (x) { return ids.indexOf(x.idPedido) !== -1; });
  }

  async function abrirCuenta() {
    await ui.conBoton(document.getElementById("botonAbrirCuenta"), async function () {
      try {
        await App.api.entidad("cuentas").crear({
          idMesa: idMesa, estado: cfg.ESTADOS.CUENTA.ABIERTA,
          fechaApertura: ui.ahoraLocal(), fechaCierre: null, total: 0
        });
        await App.datos.cambiarEstadoMesa(mesa, cfg.ESTADOS.MESA.OCUPADA);
        await App.api.auditar("cuentas", "INSERT", null, { idMesa: idMesa });
        ui.aviso("Cuenta abierta para la mesa " + mesa.numeroMesa + ".", "exito");
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
    });
  }

  /* Si la cuenta aún no tiene pedido, se crea uno antes del primer detalle. */
  async function asegurarPedido() {
    if (pedido) return pedido;
    var idUsuario = await App.sesion.idUsuario();
    await App.api.entidad("pedidos").crear({
      idCuenta: cuenta.idCuenta, idUsuario: idUsuario,
      fecha: ui.ahoraLocal(), estadoPedido: cfg.ESTADOS.PEDIDO.PENDIENTE
    });
    var todos = await App.api.entidad("pedidos").listar();   // PostPedidos no siempre devuelve el id
    pedido = todos.filter(function (p) { return p.idCuenta === cuenta.idCuenta; })
                  .sort(function (a, b) { return b.idPedido - a.idPedido; })[0];
    if (!pedido) throw new Error("No se pudo crear el pedido de la cuenta.");
    return pedido;
  }

  /* Toque en un producto: suma 1 a la línea existente o crea una nueva. */
  async function agregar(idProducto) {
    var p = productos.filter(function (x) { return x.idProducto === idProducto; })[0];
    if (!p) return;
    var svc = App.api.entidad("detallePedidos");
    var pd = await asegurarPedido();
    var linea = detalles.filter(function (d) {
      return d.idProducto === idProducto && Number(d.precioUnitario) === Number(p.precioVenta);
    })[0];

    if (linea) {
      await svc.editar({ idDetalle: linea.idDetalle, idPedido: linea.idPedido, idProducto: linea.idProducto,
                         cantidad: Number(linea.cantidad) + 1, precioUnitario: linea.precioUnitario });
      linea.cantidad = Number(linea.cantidad) + 1;
    } else {
      await svc.crear({ idPedido: pd.idPedido, idProducto: idProducto, cantidad: 1, precioUnitario: Number(p.precioVenta) });
      await refrescarDetalles();          // PostDetallePedido no devuelve el id creado
    }
    pintarTicket();
  }

  async function cambiarCantidad(idDetalle, accion) {
    var svc = App.api.entidad("detallePedidos");
    var linea = detalles.filter(function (d) { return d.idDetalle === idDetalle; })[0];
    if (!linea) return;

    if (accion === "menos" && Number(linea.cantidad) <= 1) {
      await svc.eliminar(idDetalle);
      detalles = detalles.filter(function (d) { return d.idDetalle !== idDetalle; });
    } else {
      var nueva = Number(linea.cantidad) + (accion === "mas" ? 1 : -1);
      await svc.editar({ idDetalle: linea.idDetalle, idPedido: linea.idPedido, idProducto: linea.idProducto,
                         cantidad: nueva, precioUnitario: linea.precioUnitario });
      linea.cantidad = nueva;
    }
    pintarTicket();
  }

  /* Cuenta sin consumo: se cierra y se libera la mesa (no genera comprobante). */
  async function cancelarCuenta() {
    if (!confirm("¿Cancelar la cuenta de la mesa " + mesa.numeroMesa + "? No tiene productos, se cerrará sin cobro.")) return;
    await ui.conBoton(document.getElementById("botonCancelarCuenta"), async function () {
      try {
        await App.api.entidad("cuentas").editar({
          idCuenta: cuenta.idCuenta, idMesa: cuenta.idMesa, estado: cfg.ESTADOS.CUENTA.CERRADA,
          fechaApertura: cuenta.fechaApertura, fechaCierre: ui.ahoraLocal(), total: 0
        });
        await App.datos.cambiarEstadoMesa(mesa, cfg.ESTADOS.MESA.LIBRE);
        await App.api.auditar("cuentas", "UPDATE", { idCuenta: cuenta.idCuenta }, { estado: cfg.ESTADOS.CUENTA.CERRADA });
        ui.flash("Cuenta de la mesa " + mesa.numeroMesa + " cancelada.", "exito");
        window.location.href = "mesas.html";
      } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
    });
  }

  async function pagar() {
    if (!detalles.length) { ui.aviso("Esta cuenta no tiene productos.", "error"); return; }
    var idMetodo = Number(ui.valor("metodoPago"));
    if (!idMetodo) {
      ui.aviso("Selecciona el método de pago.", "error");
      document.getElementById("metodoPago").classList.add("invalido");
      document.getElementById("metodoPago").focus();
      return;
    }
    document.getElementById("metodoPago").classList.remove("invalido");

    var total = totalActual();
    if (!confirm("¿Cobrar " + ui.moneda(total) + " y cerrar la cuenta de la mesa " + mesa.numeroMesa + "?")) return;

    await encolar(async function () {
      await ui.conBoton(document.getElementById("botonPagar"), async function () {
        try {
          var ahora = ui.ahoraLocal();
          // 1) comprobante (si un intento previo ya lo creó, no se duplica)
          var existentes = await App.api.entidad("comprobantes").listar();
          var yaCobrada = existentes.filter(function (c) { return c.idCuenta === cuenta.idCuenta; })[0];
          if (!yaCobrada) {
            await App.api.entidad("comprobantes").crear({ idCuenta: cuenta.idCuenta, fecha: ahora, total: total, idMetodo: idMetodo });
          }
          // 2) salida de inventario por lo vendido
          if (cfg.OPCIONES.KARDEX_AUTOMATICO && !yaCobrada) {
            var porProducto = new Map();
            detalles.forEach(function (d) { porProducto.set(d.idProducto, (porProducto.get(d.idProducto) || 0) + Number(d.cantidad)); });
            for (var par of porProducto) {
              await App.datos.moverStock({ idProducto: par[0], tipo: "SALIDA", cantidad: par[1],
                stockActual: stock.get(par[0]) || 0, motivo: "Venta cuenta #" + cuenta.idCuenta, permitirNegativo: true });
            }
          }
          // 3) cerrar la cuenta
          await App.api.entidad("cuentas").editar({
            idCuenta: cuenta.idCuenta, idMesa: cuenta.idMesa, estado: cfg.ESTADOS.CUENTA.CERRADA,
            fechaApertura: cuenta.fechaApertura, fechaCierre: ahora, total: total
          });
          await App.api.auditar("comprobantes", "INSERT", null, { idCuenta: cuenta.idCuenta, total: total, idMetodo: idMetodo });

          // El cobro ya está registrado: si solo falla liberar la mesa, se avisa
          // pero NO se presenta el pago como fallido.
          try {
            await App.datos.cambiarEstadoMesa(mesa, cfg.ESTADOS.MESA.LIBRE);
            ui.flash("Pago de " + ui.moneda(total) + " registrado. Mesa " + mesa.numeroMesa + " liberada.", "exito");
          } catch (errMesa) {
            ui.flash("Pago de " + ui.moneda(total) + " registrado y cuenta cerrada, pero la mesa " + mesa.numeroMesa +
                     " no quedó libre: " + errMesa.message, "advertencia");
          }
          window.location.href = "mesas.html";
        } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
      });
    });
  }
})(window.App = window.App || {});
