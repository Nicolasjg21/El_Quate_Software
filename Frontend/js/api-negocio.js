/* ==========================================================================
   API-NEGOCIO.JS — Operaciones de negocio sobre la API (El Cuate)
   --------------------------------------------------------------------------
   Todas las pantallas operativas (Mesas, Inventario, Historiales, Panel y
   Analíticas) usan este módulo; nada se guarda en localStorage.

   El Backend expone CRUD por tabla y NO encadena operaciones, por eso aquí se
   orquestan en el orden correcto:

     Vender    : Cuenta (Abierta) -> Pedido (En curso) -> DetallePedido
                 + Kardex SALIDA (el stock real = último stockNuevo del Kardex)
     Cobrar    : Comprobante (idMetodo) -> Cuenta (Cerrada) -> Pedido (Pagado)
                 -> Mesa (Libre)
     Comprar   : Compra -> DetalleCompra (por producto) + Kardex ENTRADA
     Auditoría : Api.auditar() tras cada alta / cambio / baja

   Cada llamada rechaza con { status, mensaje, errores } (ver api-cliente.js).
   Requiere: config.js, api-cliente.js y auth.js.
   ========================================================================== */
(function () {
  "use strict";

  const IVA = 0.19;
  const ESTADO = {
    mesaLibre: "Libre", mesaOcupada: "Ocupada", mesaCerrando: "Cerrando",
    cuentaAbierta: "Abierta", cuentaCerrada: "Cerrada",
    pedidoCurso: "En curso", pedidoPagado: "Pagado"
  };

  /* ---------- Utilidades ---------- */
  const num = (v) => Number(v) || 0;
  const ahora = () => Api.fechaLocal();
  const norm = (t) => String(t === undefined || t === null ? "" : t)
    .normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();
  const falla = (status, mensaje) => ({ status, mensaje, errores: {} });
  const datos = (r) => (r && r.data) || null;
  const idUsuarioActual = () => {
    const u = Auth.usuario();
    if (!u || !u.idUsuario) throw falla(401, "No fue posible identificar al usuario de la sesión.");
    return u.idUsuario;
  };

  function claveEstadoMesa(estado) {
    const e = norm(estado);
    if (e.startsWith("ocup")) return "ocupada";
    if (e.startsWith("cerr")) return "cerrando";
    return "libre";
  }

  function totales(items) {
    const subtotal = items.reduce((a, i) => a + num(i.cantidad) * num(i.precioUnitario), 0);
    const iva = Math.round(subtotal * IVA);
    return { subtotal, iva, total: subtotal + iva };
  }

  /* ==========================================================================
     CATÁLOGOS
     ========================================================================== */
  /** Productos con stock, categoría, costo promedio y proveedor.
      Filtrar (sin filtros) trae stock/costo/proveedor; GetProductos trae mínimo, estado y categoría (id). */
  async function productos() {
    const [lista, detalle] = await Promise.all([
      Api.lista("api/Productos/Filtrar"),
      Api.lista("api/Productos/GetProductos")
    ]);
    const det = new Map(detalle.map((d) => [d.idProducto, d]));
    return lista.map((p) => {
      const d = det.get(p.idProducto) || {};
      return {
        idProducto: p.idProducto,
        nombre: p.nombreProducto || "",
        precio: num(p.precioVenta),
        costo: num(p.costoPromedio),
        stock: num(p.stockActual),
        stockMinimo: num(d.cantidadMinima),
        stockBajo: !!p.stockBajo,
        categoria: p.categoria || "Sin categoría",
        idCategoria: d.idCategoria || 0,
        proveedor: p.proveedor || "",
        activo: d.estado !== false
      };
    });
  }

  const categorias = () => Api.lista("api/Categorias/GetCategorias");
  const proveedores = () => Api.lista("api/Proveedores/GetProveedores");
  const metodosPago = () => Api.lista("api/MetodosPago/GetMetodosPago");

  async function crearCategoria(nombreCategoria) {
    const r = datos(await Api.post("api/Categorias/PostCategoria", { nombreCategoria }));
    Api.auditar("Categorias", "INSERT", null, { nombreCategoria });
    return r;
  }

  async function crearMetodoPago(nombreMetodo) {
    const r = datos(await Api.post("api/MetodosPago/PostMetodosPago", { nombreMetodo }));
    Api.auditar("MetodosPago", "INSERT", null, { nombreMetodo });
    return r;
  }

  /* ==========================================================================
     KARDEX (el stock actual es el stockNuevo del último movimiento)
     ========================================================================== */
  function registrarKardex(idProducto, tipoMovimiento, cantidad, stockAnterior, stockNuevo, motivo) {
    return Api.post("api/Kardex/PostKardex", {
      idProducto, tipoMovimiento, cantidad, stockAnterior, stockNuevo,
      motivo: motivo ? String(motivo).slice(0, 500) : null,
      fecha: ahora()
    });
  }

  /** Aplica delta (+entrada / -salida) sobre el stock más reciente del servidor. */
  async function moverStock(idProducto, delta, motivo) {
    const lista = await Api.lista("api/Productos/Filtrar");
    const p = lista.find((x) => x.idProducto === idProducto);
    if (!p) throw falla(404, "El producto ya no existe.");
    const anterior = num(p.stockActual);
    const nuevo = anterior + delta;
    if (nuevo < 0) {
      throw falla(409, `Stock insuficiente de "${p.nombreProducto}" (disponible: ${anterior}).`);
    }
    await registrarKardex(idProducto, delta < 0 ? "SALIDA" : "ENTRADA", Math.abs(delta), anterior, nuevo, motivo);
    return nuevo;
  }

  /** Ajuste manual del stock a un valor exacto (solo administración). */
  async function ajustarStock(idProducto, stockObjetivo, motivo) {
    const lista = await Api.lista("api/Productos/Filtrar");
    const p = lista.find((x) => x.idProducto === idProducto);
    if (!p) throw falla(404, "El producto ya no existe.");
    const anterior = num(p.stockActual);
    if (anterior === stockObjetivo) return anterior;
    await registrarKardex(idProducto, "AJUSTE", Math.abs(stockObjetivo - anterior), anterior, stockObjetivo, motivo || "Ajuste manual de inventario");
    Api.auditar("Inventario", "UPDATE", { idProducto, producto: p.nombreProducto, stock: anterior }, { idProducto, producto: p.nombreProducto, stock: stockObjetivo });
    return stockObjetivo;
  }

  /* ==========================================================================
     PRODUCTOS (inventario)
     ========================================================================== */
  /** Crea el producto y, si hay stock inicial, lo registra como Compra (si hay proveedor y costo) o como entrada de Kardex. */
  async function crearProducto(p) {
    const creado = datos(await Api.post("api/Productos/PostProductos", {
      nombreProducto: p.nombre, precioVenta: p.precio, cantidadMinima: p.stockMinimo,
      estado: true, idCategoria: p.idCategoria
    }));
    if (!creado || !creado.idProducto) throw falla(500, "El servidor no devolvió el producto creado.");
    Api.auditar("Productos", "INSERT", null, {
      idProducto: creado.idProducto, nombreProducto: p.nombre, precioVenta: p.precio, cantidadMinima: p.stockMinimo, idCategoria: p.idCategoria
    });

    const aviso = [];
    if (p.stockInicial > 0) {
      try {
        if (p.idProveedor && p.costo !== undefined && p.costo !== null && p.costo !== "") {
          const rc = await crearCompra({
            idProveedor: p.idProveedor, fecha: null,
            items: [{ idProducto: creado.idProducto, cantidad: p.stockInicial, costoUnitario: Number(p.costo) }]
          });
          (rc.aviso || []).forEach((a) => aviso.push(a));
        } else {
          await registrarKardex(creado.idProducto, "ENTRADA", p.stockInicial, 0, p.stockInicial, "Stock inicial");
        }
      } catch (e) {
        aviso.push(`El producto se creó, pero no se pudo registrar el stock inicial: ${e && e.mensaje ? e.mensaje : "error desconocido"}. Regístrelo con "Ajustar stock".`);
      }
    }
    return { producto: creado, aviso };
  }

  async function actualizarProducto(anterior, cambios) {
    const cuerpo = {
      idProducto: anterior.idProducto,
      nombreProducto: cambios.nombre, precioVenta: cambios.precio, cantidadMinima: cambios.stockMinimo,
      estado: cambios.activo !== false, idCategoria: cambios.idCategoria
    };
    const r = datos(await Api.put("api/Productos/PutProductos", cuerpo));
    Api.auditar("Productos", "UPDATE",
      { idProducto: anterior.idProducto, nombreProducto: anterior.nombre, precioVenta: anterior.precio, cantidadMinima: anterior.stockMinimo, idCategoria: anterior.idCategoria },
      { idProducto: anterior.idProducto, nombreProducto: cambios.nombre, precioVenta: cambios.precio, cantidadMinima: cambios.stockMinimo, idCategoria: cambios.idCategoria });
    return r;
  }

  async function eliminarProducto(producto) {
    await Api.del("api/Productos/DeleteProductos/" + producto.idProducto);
    Api.auditar("Productos", "DELETE", { idProducto: producto.idProducto, nombreProducto: producto.nombre, precioVenta: producto.precio }, null);
    return true;
  }

  /* ==========================================================================
     MESAS / CUENTAS / PEDIDOS
     ========================================================================== */
  async function cargarMesas() {
    const [mesas, cuentas] = await Promise.all([
      Api.lista("api/Mesas/GetMesas"),
      Api.lista("api/Cuentas/GetCuentas")
    ]);
    const abiertas = new Map();
    cuentas
      .filter((c) => norm(c.estado) === "abierta")
      .sort((a, b) => a.idCuenta - b.idCuenta)
      .forEach((c) => abiertas.set(c.idMesa, c));
    return mesas
      .map((m) => ({
        idMesa: m.idMesa, numero: m.numeroMesa,
        estado: claveEstadoMesa(m.estado), estadoTexto: m.estado,
        cuenta: abiertas.get(m.idMesa) || null
      }))
      .sort((a, b) => a.numero - b.numero);
  }

  /** Detalle de la cuenta abierta de una mesa: pedidos, líneas y comprobantes ya emitidos. */
  async function cargarDetalle(mesa) {
    const vacio = { cuenta: mesa.cuenta, pedidos: [], items: [], comprobantes: [] };
    if (!mesa.cuenta) return vacio;

    const todos = await Api.solicitar("POST", "api/Pedidos/filtrar", { cuerpo: { numeroMesa: mesa.numero } });
    const pedidos = (Array.isArray(todos) ? todos : []).filter((p) => p.idCuenta === mesa.cuenta.idCuenta);
    if (!pedidos.length) return vacio;

    const ids = new Set(pedidos.map((p) => p.idPedido));
    const detalles = await Api.lista("api/DetallePedidos/GetDetallePedidos");
    const comprobantes = (pedidos[0].cuenta && pedidos[0].cuenta.comprobantes) || [];
    return {
      cuenta: mesa.cuenta,
      pedidos,
      items: detalles.filter((d) => ids.has(d.idPedido)).map((d) => ({
        idDetalle: d.idDetalle, idPedido: d.idPedido, idProducto: d.idProducto,
        cantidad: num(d.cantidad), precioUnitario: num(d.precioUnitario)
      })),
      comprobantes
    };
  }

  function cuerpoMesa(mesa, estado) {
    return { idMesa: mesa.idMesa, numeroMesa: mesa.numero, estado };
  }

  async function cambiarEstadoMesa(mesa, estado) {
    await Api.put("api/Mesas/PutMesa", cuerpoMesa(mesa, estado));
    const anterior = mesa.estadoTexto;
    mesa.estadoTexto = estado;
    mesa.estado = claveEstadoMesa(estado);
    Api.auditar("Mesas", "UPDATE", { numeroMesa: mesa.numero, estado: anterior }, { numeroMesa: mesa.numero, estado });
  }

  async function sincronizarCuenta(detalle) {
    const c = detalle.cuenta;
    const t = totales(detalle.items);
    await Api.put("api/Cuentas/PutCuenta", {
      idCuenta: c.idCuenta, idMesa: c.idMesa, estado: c.estado,
      fechaApertura: c.fechaApertura, fechaCierre: c.fechaCierre || null, total: t.total
    });
    c.total = t.total;
  }

  async function asegurarCuentaYPedido(mesa, detalle) {
    if (!mesa.cuenta) {
      const c = datos(await Api.post("api/Cuentas/PostCuenta", {
        idMesa: mesa.idMesa, estado: ESTADO.cuentaAbierta, fechaApertura: ahora(), fechaCierre: null, total: 0
      }));
      if (!c || !c.idCuenta) throw falla(500, "El servidor no devolvió la cuenta creada.");
      mesa.cuenta = c;
      detalle.cuenta = c;
      Api.auditar("Cuentas", "INSERT", null, { idCuenta: c.idCuenta, numeroMesa: mesa.numero, estado: ESTADO.cuentaAbierta });
    }
    if (!detalle.pedidos.length) {
      const p = datos(await Api.post("api/Pedidos/PostPedidos", {
        idCuenta: mesa.cuenta.idCuenta, idUsuario: idUsuarioActual(), fecha: ahora(), estadoPedido: ESTADO.pedidoCurso
      }));
      if (!p || !p.idPedido) throw falla(500, "El servidor no devolvió el pedido creado.");
      detalle.pedidos.push(p);
      Api.auditar("Pedidos", "INSERT", null, { idPedido: p.idPedido, idCuenta: p.idCuenta, numeroMesa: mesa.numero });
    }
  }

  /** Agrega 1 unidad del producto a la mesa (descuenta stock en Kardex). Devuelve el detalle actualizado. */
  async function agregarProducto(mesa, detalle, producto) {
    await moverStock(producto.idProducto, -1, `Venta en Mesa ${mesa.numero}`);
    try {
      await asegurarCuentaYPedido(mesa, detalle);
      const existente = detalle.items.find((i) => i.idProducto === producto.idProducto);
      if (existente) {
        await Api.put("api/DetallePedidos/PutDetallePedido", {
          idDetalle: existente.idDetalle, idPedido: existente.idPedido, idProducto: existente.idProducto,
          cantidad: existente.cantidad + 1, precioUnitario: existente.precioUnitario
        });
        existente.cantidad += 1;
      } else {
        const idPedido = detalle.pedidos[0].idPedido;
        const d = datos(await Api.post("api/DetallePedidos/PostDetallePedido", {
          idPedido, idProducto: producto.idProducto, cantidad: 1, precioUnitario: producto.precio
        }));
        detalle.items.push({
          idDetalle: d && d.idDetalle, idPedido, idProducto: producto.idProducto, cantidad: 1, precioUnitario: producto.precio
        });
      }
      await sincronizarCuenta(detalle);
      if (mesa.estado === "libre") await cambiarEstadoMesa(mesa, ESTADO.mesaOcupada);
      return detalle;
    } catch (e) {
      /* El pedido no quedó registrado: se devuelve la unidad al stock. */
      try { await moverStock(producto.idProducto, 1, `Reversión: no se registró el pedido de Mesa ${mesa.numero}`); } catch (_) {}
      throw e;
    }
  }

  /** Cambia la cantidad de una línea en +1 / -1. Al llegar a 0 la línea se elimina. */
  async function cambiarCantidad(mesa, detalle, idDetalle, delta) {
    const item = detalle.items.find((i) => i.idDetalle === idDetalle);
    if (!item) throw falla(404, "La línea ya no existe.");
    if (delta > 0) await moverStock(item.idProducto, -delta, `Venta en Mesa ${mesa.numero}`);

    try {
      const nueva = item.cantidad + delta;
      if (nueva <= 0) {
        await Api.del("api/DetallePedidos/DeleteDetallePedido/" + idDetalle);
        detalle.items = detalle.items.filter((i) => i.idDetalle !== idDetalle);
      } else {
        await Api.put("api/DetallePedidos/PutDetallePedido", {
          idDetalle, idPedido: item.idPedido, idProducto: item.idProducto, cantidad: nueva, precioUnitario: item.precioUnitario
        });
        item.cantidad = nueva;
      }
    } catch (e) {
      if (delta > 0) { try { await moverStock(item.idProducto, delta, `Reversión: no se registró el pedido de Mesa ${mesa.numero}`); } catch (_) {} }
      throw e;
    }
    if (delta < 0) await moverStock(item.idProducto, -delta, `Devolución desde Mesa ${mesa.numero}`);
    await sincronizarCuenta(detalle);
    return detalle;
  }

  async function quitarItem(mesa, detalle, idDetalle) {
    const item = detalle.items.find((i) => i.idDetalle === idDetalle);
    if (!item) throw falla(404, "La línea ya no existe.");
    await Api.del("api/DetallePedidos/DeleteDetallePedido/" + idDetalle);
    detalle.items = detalle.items.filter((i) => i.idDetalle !== idDetalle);
    await moverStock(item.idProducto, item.cantidad, `Devolución desde Mesa ${mesa.numero}`);
    await sincronizarCuenta(detalle);
    return detalle;
  }

  /** La mesa pasa a "Cerrando" (cuenta lista para cobrar). */
  async function confirmarPago(mesa, detalle) {
    if (!detalle.items.length) throw falla(400, "La mesa no tiene productos para cobrar.");
    await sincronizarCuenta(detalle);
    await cambiarEstadoMesa(mesa, ESTADO.mesaCerrando);
  }

  async function reabrirCuenta(mesa) {
    await cambiarEstadoMesa(mesa, ESTADO.mesaOcupada);
  }

  /** Cobro: Comprobante -> Cuenta cerrada -> Pedidos pagados -> Mesa libre. Reintentable. */
  async function cobrar(mesa, detalle, idMetodo) {
    if (!detalle.items.length || !detalle.cuenta) throw falla(400, "La mesa no tiene una cuenta con productos para cobrar.");
    if (!idMetodo) throw falla(400, "Seleccione un método de pago.");
    const t = totales(detalle.items);
    const c = detalle.cuenta;

    if (!detalle.comprobantes.length) {
      const comp = datos(await Api.post("api/Comprobantes/PostComprobante", {
        idCuenta: c.idCuenta, fecha: ahora(), total: t.total, idMetodo
      }));
      detalle.comprobantes.push(comp || {});
      Api.auditar("Comprobantes", "INSERT", null, { idCuenta: c.idCuenta, numeroMesa: mesa.numero, total: t.total, idMetodo });
    }

    await Api.put("api/Cuentas/PutCuenta", {
      idCuenta: c.idCuenta, idMesa: c.idMesa, estado: ESTADO.cuentaCerrada,
      fechaApertura: c.fechaApertura, fechaCierre: ahora(), total: t.total
    });
    for (const p of detalle.pedidos) {
      await Api.put("api/Pedidos/PutPedidos", {
        idPedido: p.idPedido, idCuenta: p.idCuenta, idUsuario: p.idUsuario, fecha: p.fecha, estadoPedido: ESTADO.pedidoPagado
      });
    }
    await cambiarEstadoMesa(mesa, ESTADO.mesaLibre);
    Api.auditar("Cuentas", "UPDATE", { idCuenta: c.idCuenta, estado: ESTADO.cuentaAbierta }, { idCuenta: c.idCuenta, estado: ESTADO.cuentaCerrada, total: t.total });
    mesa.cuenta = null;
  }

  async function crearMesa(numeroMesa) {
    const r = datos(await Api.post("api/Mesas/PostMesa", { numeroMesa, estado: ESTADO.mesaLibre }));
    Api.auditar("Mesas", "INSERT", null, { numeroMesa, estado: ESTADO.mesaLibre });
    return r;
  }

  async function eliminarMesa(mesa) {
    await Api.del("api/Mesas/DeleteMesa/" + mesa.idMesa);
    Api.auditar("Mesas", "DELETE", { numeroMesa: mesa.numero, estado: mesa.estadoTexto }, null);
  }

  /* ==========================================================================
     HISTORIAL DE ÓRDENES (cuentas cobradas)
     ========================================================================== */
  const idOrden = (idCuenta) => "ORD-" + String(idCuenta).padStart(6, "0");

  async function ordenesPagadas() {
    const pedidos = await Api.solicitar("POST", "api/Pedidos/filtrar", { cuerpo: {} });
    const porCuenta = new Map();
    (Array.isArray(pedidos) ? pedidos : []).forEach((p) => {
      const c = p.cuenta;
      if (!c || !c.comprobantes || !c.comprobantes.length) return;
      let o = porCuenta.get(c.idCuenta);
      if (!o) {
        const comp = c.comprobantes[0];
        o = {
          id: idOrden(c.idCuenta), idCuenta: c.idCuenta,
          mesa: c.mesa ? c.mesa.numeroMesa : "—",
          mesero: p.usuario ? `${p.usuario.nombres} ${p.usuario.apellidos}`.trim() : `Usuario #${p.idUsuario}`,
          fechaHora: comp.fecha || c.fechaCierre || p.fecha,
          total: c.comprobantes.reduce((a, x) => a + num(x.total), 0),
          metodoPago: (comp.metodoPago && comp.metodoPago.nombreMetodo) || "—",
          estado: "Pagada", idPedidos: []
        };
        porCuenta.set(c.idCuenta, o);
      }
      o.idPedidos.push(p.idPedido);
    });
    return [...porCuenta.values()].sort((a, b) => String(b.fechaHora).localeCompare(String(a.fechaHora)));
  }

  /** Líneas de una orden con nombre de producto. */
  async function itemsOrden(orden, catalogo) {
    const [detalles, prods] = await Promise.all([
      Api.lista("api/DetallePedidos/GetDetallePedidos"),
      catalogo ? Promise.resolve(catalogo) : productos()
    ]);
    const ids = new Set(orden.idPedidos);
    const mapa = new Map(prods.map((p) => [p.idProducto, p]));
    return detalles.filter((d) => ids.has(d.idPedido)).map((d) => ({
      nombre: (mapa.get(d.idProducto) || {}).nombre || `Producto #${d.idProducto}`,
      cantidad: num(d.cantidad), precioUnitario: num(d.precioUnitario)
    }));
  }

  /* ==========================================================================
     COMPRAS
     ========================================================================== */
  const idCompraTexto = (id) => "CMP-" + String(id).padStart(6, "0");

  async function listarCompras() {
    const [compras, provs, detalles, prods] = await Promise.all([
      Api.lista("api/Compras/GetCompras"),
      proveedores(),
      Api.lista("api/DetalleCompras/GetDetalleCompras"),
      productos()
    ]);
    const nomProv = new Map(provs.map((p) => [p.idProveedor, p.nombreProveedor]));
    const mapaProd = new Map(prods.map((p) => [p.idProducto, p]));
    return compras.map((c) => {
      const items = detalles.filter((d) => d.idCompra === c.idCompra).map((d) => {
        const p = mapaProd.get(d.idProducto) || {};
        return {
          nombre: p.nombre || `Producto #${d.idProducto}`, categoria: p.categoria || "—",
          cantidad: num(d.cantidad), costoUnitario: num(d.precioCompra)
        };
      });
      return {
        id: idCompraTexto(c.idCompra), idCompra: c.idCompra,
        fecha: c.fecha, proveedor: nomProv.get(c.idProveedor) || `Proveedor #${c.idProveedor}`,
        items, costoTotal: num(c.total)
      };
    }).sort((a, b) => b.idCompra - a.idCompra);
  }

  /** items: [{ idProducto, cantidad, costoUnitario }]. fecha: "AAAA-MM-DD" o null (= ahora). */
  async function crearCompra({ idProveedor, fecha, items }) {
    const total = items.reduce((a, i) => a + i.cantidad * i.costoUnitario, 0);
    const hora = ahora().split("T")[1];
    const fechaCompra = fecha ? `${fecha}T${hora}` : ahora();

    const compra = datos(await Api.post("api/Compras/PostCompra", { idProveedor, fecha: fechaCompra, total }));
    if (!compra || !compra.idCompra) throw falla(500, "El servidor no devolvió la compra creada.");

    const creados = [];
    try {
      for (const it of items) {
        const d = datos(await Api.post("api/DetalleCompras/PostDetalleCompra", {
          idCompra: compra.idCompra, idProducto: it.idProducto, cantidad: it.cantidad, precioCompra: it.costoUnitario
        }));
        creados.push(d);
      }
    } catch (e) {
      /* Deshacer lo creado para no dejar una compra incompleta. */
      for (const d of creados) { try { if (d && d.idDetalleCompra) await Api.del("api/DetalleCompras/DeleteDetalleCompra/" + d.idDetalleCompra); } catch (_) {} }
      try { await Api.del("api/Compras/DeleteCompra/" + compra.idCompra); } catch (_) {}
      throw e;
    }

    const aviso = [];
    for (const it of items) {
      try { await moverStock(it.idProducto, it.cantidad, `Compra ${idCompraTexto(compra.idCompra)}`); }
      catch (e) { aviso.push(`No se pudo actualizar el stock del producto #${it.idProducto}: ${e && e.mensaje ? e.mensaje : "error"}`); }
    }
    Api.auditar("Compras", "INSERT", null, { idCompra: compra.idCompra, idProveedor, total, productos: items.length });
    return { compra, aviso };
  }

  /* ==========================================================================
     ANALÍTICAS
     ========================================================================== */
  function ventas(desde, hasta) {
    return Api.get("api/Analiticas/Ventas", { fechaDesde: desde || undefined, fechaHasta: hasta || undefined })
      .then((r) => {
        const d = (r && r.data) || {};
        return {
          totalVentas: num(d.totalVentas), totalIngresos: num(d.totalIngresos),
          promedioVenta: num(d.promedioVenta), totalProductosVendidos: num(d.totalProductosVendidos)
        };
      });
  }

  /** Consumo por producto y categoría de los pedidos pagados del período (antes de IVA). */
  async function consumoPeriodo(desde, hasta) {
    const [hist, detalles, prods] = await Promise.all([
      Api.get("api/Pedidos/Historial", { fechaDesde: desde || undefined, fechaHasta: hasta || undefined }).then((r) => (r && r.data) || []),
      Api.lista("api/DetallePedidos/GetDetallePedidos"),
      productos()
    ]);
    const pagados = new Set(hist.filter((p) => norm(p.estadoPedido) === "pagado").map((p) => p.idPedido));
    const mapa = new Map(prods.map((p) => [p.idProducto, p]));
    const porProducto = new Map();
    detalles.filter((d) => pagados.has(d.idPedido)).forEach((d) => {
      const p = mapa.get(d.idProducto) || {};
      const fila = porProducto.get(d.idProducto) || { nombre: p.nombre || `Producto #${d.idProducto}`, categoria: p.categoria || "Sin categoría", unidades: 0, ingresos: 0 };
      fila.unidades += num(d.cantidad);
      fila.ingresos += num(d.cantidad) * num(d.precioUnitario);
      porProducto.set(d.idProducto, fila);
    });
    const filas = [...porProducto.values()].sort((a, b) => b.unidades - a.unidades);
    const porCategoria = new Map();
    filas.forEach((f) => porCategoria.set(f.categoria, (porCategoria.get(f.categoria) || 0) + f.ingresos));
    return {
      productos: filas,
      categorias: [...porCategoria.entries()].map(([nombre, ingresos]) => ({ nombre, ingresos })).sort((a, b) => b.ingresos - a.ingresos),
      pedidos: hist.filter((p) => pagados.has(p.idPedido))
    };
  }

  window.Negocio = {
    IVA, ESTADO, norm, totales, claveEstadoMesa,
    productos, categorias, proveedores, metodosPago, crearCategoria, crearMetodoPago,
    ajustarStock, crearProducto, actualizarProducto, eliminarProducto,
    cargarMesas, cargarDetalle, agregarProducto, cambiarCantidad, quitarItem,
    confirmarPago, reabrirCuenta, cobrar, crearMesa, eliminarMesa,
    ordenesPagadas, itemsOrden, listarCompras, crearCompra,
    ventas, consumoPeriodo
  };
})();
