/* ==========================================================================
   DATOS — lógica compartida entre pantallas
   --------------------------------------------------------------------------
   Los repositories del backend devuelven entidades PLANAS (sin Include): un
   producto llega con idCategoria pero sin `categoria`. Por eso las llaves
   foráneas se resuelven aquí, en el cliente, con mapas por id.
   ========================================================================== */
(function (App) {
  "use strict";

  var cfg = App.config;
  var ui = App.ui;
  var datos = {};

  /* Carga varias listas en paralelo: cargar(["productos","categorias"]) → { productos:[], categorias:[] } */
  datos.cargar = async function (claves) {
    var resultados = await Promise.all(claves.map(function (c) { return App.api.entidad(c).listar(); }));
    var salida = {};
    claves.forEach(function (c, i) { salida[c] = resultados[i]; });
    return salida;
  };

  /* Stock actual por producto = stockNuevo del ÚLTIMO movimiento del Kardex.
     No existe columna "stock" en Productos; el Kardex es la fuente de verdad. */
  datos.stockPorProducto = function (kardex) {
    var ultimo = new Map();
    kardex.forEach(function (m) {
      var previo = ultimo.get(m.idProducto);
      if (!previo) { ultimo.set(m.idProducto, m); return; }
      var fm = ui.parseFecha(m.fecha), fp = ui.parseFecha(previo.fecha);
      var tm = fm ? fm.getTime() : 0, tp = fp ? fp.getTime() : 0;
      if (tm > tp || (tm === tp && m.idMovimiento > previo.idMovimiento)) ultimo.set(m.idProducto, m);
    });
    var stock = new Map();
    ultimo.forEach(function (m, id) { stock.set(id, Number(m.stockNuevo) || 0); });
    return stock;
  };

  /* Último precio de compra por producto (el precio de compra vive en DetalleCompras). */
  datos.ultimoCosto = function (detalleCompras) {
    var costo = new Map(), idMax = new Map();
    detalleCompras.forEach(function (d) {
      if (!idMax.has(d.idProducto) || d.idDetalleCompra > idMax.get(d.idProducto)) {
        idMax.set(d.idProducto, d.idDetalleCompra);
        costo.set(d.idProducto, d.precioCompra);
      }
    });
    return costo;
  };

  /* Cuenta abierta de una mesa (la más reciente). */
  datos.cuentaAbierta = function (cuentas, idMesa) {
    var abiertas = cuentas.filter(function (c) {
      return c.idMesa === idMesa && cfg.igual(c.estado, cfg.ESTADOS.CUENTA.ABIERTA);
    });
    abiertas.sort(function (a, b) { return b.idCuenta - a.idCuenta; });
    return abiertas[0] || null;
  };

  /* Detalles agrupados por cuenta: Map idCuenta → [detalle…] (cadena Detalle → Pedido → Cuenta). */
  datos.detallesPorCuenta = function (pedidos, detalles) {
    var cuentaDePedido = new Map();
    pedidos.forEach(function (p) { cuentaDePedido.set(p.idPedido, p.idCuenta); });
    var porCuenta = new Map();
    detalles.forEach(function (d) {
      var idCuenta = cuentaDePedido.get(d.idPedido);
      if (idCuenta == null) return;
      if (!porCuenta.has(idCuenta)) porCuenta.set(idCuenta, []);
      porCuenta.get(idCuenta).push(d);
    });
    return porCuenta;
  };

  datos.sumar = function (detalles) {
    return ui.redondear((detalles || []).reduce(function (s, d) { return s + Number(d.cantidad) * Number(d.precioUnitario); }, 0));
  };

  /* ---------- Kardex ---------- */
  /* Registra un movimiento calculando stockAnterior/stockNuevo.
     tipo: "ENTRADA" | "SALIDA" (se traduce con config.ESTADOS.KARDEX). */
  datos.moverStock = async function (o) {
    var esEntrada = o.tipo === "ENTRADA";
    var cantidad = Number(o.cantidad);
    if (!(cantidad > 0) || Math.floor(cantidad) !== cantidad) throw new Error("La cantidad del movimiento debe ser un entero mayor que cero.");
    var anterior = Number(o.stockActual) || 0;
    var nuevo = esEntrada ? anterior + cantidad : anterior - cantidad;
    if (nuevo < 0 && !o.permitirNegativo) {
      throw new Error("Stock insuficiente: hay " + anterior + " unidades y se intentó sacar " + cantidad + ".");
    }
    var idUsuario = await App.sesion.idUsuario();
    await App.api.entidad("kardex").crear({
      idProducto: o.idProducto,
      tipoMovimiento: esEntrada ? cfg.ESTADOS.KARDEX.ENTRADA : cfg.ESTADOS.KARDEX.SALIDA,
      cantidad: cantidad,
      stockAnterior: anterior,
      stockNuevo: nuevo,
      motivo: o.motivo ? String(o.motivo).slice(0, cfg.REGLAS.MOTIVO_MAX) : null,
      fecha: ui.ahoraLocal(),
      idUsuario: idUsuario
    });
    return nuevo;
  };

  /* ---------- Mesas ---------- */
  /* PutMesa del backend responde 200 pero NO copia los campos recibidos (ver informe, B2).
     Para no mostrar un "éxito" falso, se vuelve a leer la mesa y se comprueba el cambio. */
  datos.cambiarEstadoMesa = async function (mesa, nuevoEstado) {
    var svc = App.api.entidad("mesas");
    await svc.editar({ idMesa: mesa.idMesa, numeroMesa: mesa.numeroMesa, estado: nuevoEstado });
    var actual = await svc.porId(mesa.idMesa);
    if (actual && !cfg.igual(actual.estado, nuevoEstado)) {
      throw new Error("PutMesa respondió OK pero la mesa " + mesa.numeroMesa + " sigue «" + actual.estado + "» en la base de datos. " +
        "El método PutMesa del backend no copia los campos recibidos (MesasController: falta existente.estado = mesas.estado). Ver informe, hallazgo B2.");
    }
    mesa.estado = nuevoEstado;
    return actual || mesa;
  };

  App.datos = datos;
})(window.App = window.App || {});
