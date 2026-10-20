/**
 * @file api-negocio.js
 * @description Capa de negocio sobre la API. El Backend expone operaciones por tabla y no
 *   encadena procesos; este módulo los orquesta en el orden correcto:
 *     Venta    Cuenta (Abierta) → Pedido (En curso) → DetallePedido + Kardex SALIDA
 *     Cobro    Comprobante → Cuenta (Cerrada) → Pedidos (Pagado) → Mesa (Libre)
 *     Compra   Compra → DetalleCompra por producto + Kardex ENTRADA
 *   El stock de un producto es el stockNuevo de su último movimiento en Kardex.
 *   Cada alta, cambio o baja registra su auditoría.
 * @requires api-cliente.js, auth.js
 * @exports window.Negocio
 */

(function () {
  "use strict";

  /** Tarifa de IVA aplicada a las cuentas. Los valores se manejan en pesos colombianos (COP). */
  const IVA = 0.19;

  /** Valores de estado tal como se almacenan en Mesas.estado, Cuentas.estado y Pedidos.estadoPedido. */
  const ESTADO = {
    mesaLibre: "Libre", mesaOcupada: "Ocupada", mesaCerrando: "Cerrando", mesaInactiva: "Inactiva",
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

  /** Id del usuario de la sesión; requerido por PedidoCrearDTO. */
  const idUsuarioActual = () => {
    const u = Auth.usuario();
    if (!u || !u.idUsuario) throw falla(401, "No fue posible identificar al usuario de la sesión.");
    return u.idUsuario;
  };

  /**
   * Normaliza el estado de una mesa a: libre, ocupada, cerrando o inactiva (retirada del salón).
   * @param {string} estado Valor almacenado.
   * @returns {string}
   */
  function claveEstadoMesa(estado) {
    const e = norm(estado);
    if (e.startsWith("ocup")) return "ocupada";
    if (e.startsWith("cerr")) return "cerrando";
    if (e.startsWith("inac") || e.startsWith("retir")) return "inactiva";
    return "libre";
  }

  /**
   * Totales de una cuenta: subtotal, IVA (redondeado al peso) y total.
   * @param {Array<{cantidad:number, precioUnitario:number}>} items
   * @returns {{subtotal:number, iva:number, total:number}}
   */
  function totales(items) {
    const subtotal = items.reduce((a, i) => a + num(i.cantidad) * num(i.precioUnitario), 0);
    const iva = Math.round(subtotal * IVA);
    return { subtotal, iva, total: subtotal + iva };
  }

  /* ==================== Catálogos ==================== */

  /**
   * Productos con stock, costo promedio, proveedor y categoría.
   * Combina Productos/Filtrar (stock, costo, proveedor, nombre de categoría) con
   * Productos/GetProductos (stock mínimo, estado e idCategoria), porque ninguno trae todos los datos.
   * @returns {Promise<Array<object>>}
   */
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

  /** Categorías y métodos de pago. */
  async function crearCategoria(nombreCategoria) {
    const r = datos(await Api.post("api/Categorias/PostCategoria", { nombreCategoria }));
    Api.auditar("Categorias", "INSERT", null, { nombreCategoria });
    return r;
  }

  async function actualizarCategoria(categoria, nombreCategoria) {
    await Api.put("api/Categorias/PutCategoria", { idCategoria: categoria.idCategoria, nombreCategoria });
    Api.auditar("Categorias", "UPDATE",
      { idCategoria: categoria.idCategoria, nombreCategoria: categoria.nombreCategoria },
      { idCategoria: categoria.idCategoria, nombreCategoria });
  }

  async function eliminarCategoria(categoria) {
    await Api.del("api/Categorias/DeleteCategoria/" + categoria.idCategoria);
    Api.auditar("Categorias", "DELETE", { idCategoria: categoria.idCategoria, nombreCategoria: categoria.nombreCategoria }, null);
  }

  async function crearMetodoPago(nombreMetodo) {
    const r = datos(await Api.post("api/MetodosPago/PostMetodosPago", { nombreMetodo }));
    Api.auditar("MetodosPago", "INSERT", null, { nombreMetodo });
    return r;
  }

  /* ==================== Kardex ==================== */

  /** Registra un movimiento de inventario (ENTRADA, SALIDA o AJUSTE). El Backend asigna el usuario. */
  function registrarKardex(idProducto, tipoMovimiento, cantidad, stockAnterior, stockNuevo, motivo) {
    return Api.post("api/Kardex/PostKardex", {
      idProducto, tipoMovimiento, cantidad, stockAnterior, stockNuevo,
      motivo: motivo ? String(motivo).slice(0, 500) : null,
      fecha: ahora()
    });
  }

  /**
   * Aplica una variación de stock sobre el valor más reciente del servidor.
   * @param {number} idProducto
   * @param {number} delta Positivo para entradas, negativo para salidas.
   * @param {string} motivo
   * @returns {Promise<number>} Stock resultante.
   * @throws 409 si el stock resultante sería negativo.
   */
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

  /** Ajuste manual del stock a un valor exacto (movimiento AJUSTE). */
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

  /* ==================== Productos ==================== */

  /**
   * Crea un producto. Si tiene stock inicial, lo registra como compra (cuando se indican proveedor y costo)
   * o como entrada de Kardex.
   * @returns {Promise<{producto:object, aviso:string[]}>} aviso: problemas no bloqueantes con el stock inicial.
   */
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

  /** Actualiza un producto conservando el estado indicado (activo o inactivo). */
  async function actualizarProducto(anterior, cambios) {
    const cuerpo = {
      idProducto: anterior.idProducto,
      nombreProducto: cambios.nombre, precioVenta: cambios.precio, cantidadMinima: cambios.stockMinimo,
      estado: cambios.activo !== false, idCategoria: cambios.idCategoria
    };
    const r = datos(await Api.put("api/Productos/PutProductos", cuerpo));
    Api.auditar("Productos", "UPDATE",
      { idProducto: anterior.idProducto, nombreProducto: anterior.nombre, precioVenta: anterior.precio, cantidadMinima: anterior.stockMinimo, idCategoria: anterior.idCategoria, estado: anterior.activo !== false },
      { idProducto: anterior.idProducto, nombreProducto: cambios.nombre, precioVenta: cambios.precio, cantidadMinima: cambios.stockMinimo, idCategoria: cambios.idCategoria, estado: cambios.activo !== false });
    return r;
  }

  async function eliminarProducto(producto) {
    await Api.del("api/Productos/DeleteProductos/" + producto.idProducto);
    Api.auditar("Productos", "DELETE", { idProducto: producto.idProducto, nombreProducto: producto.nombre, precioVenta: producto.precio }, null);
    return true;
  }

  /* ==================== Mesas, cuentas y pedidos ==================== */

  /**
   * Mesas con su cuenta abierta, si la tienen.
   * @returns {Promise<Array<{idMesa:number, numero:number, estado:string, estadoTexto:string, cuenta:object|null}>>}
   */
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
      .map((m) => {
        const cuenta = abiertas.get(m.idMesa) || null;
        const guardado = claveEstadoMesa(m.estado);
        const estado = estadoEfectivo(guardado, cuenta);
        return {
          idMesa: m.idMesa, numero: m.numeroMesa,
          estado, estadoTexto: m.estado, cuenta,
          desincronizada: guardado !== estado
        };
      })
      .sort((a, b) => a.numero - b.numero);
  }

  /**
   * Estado que se muestra para una mesa. Una mesa solo está ocupada o cerrando si tiene una cuenta
   * abierta con consumo; en cualquier otro caso está libre, aunque el estado guardado diga otra cosa.
   * El consumo se toma de Cuentas.total, que se recalcula con cada cambio en la cuenta.
   * @param {string} guardado Estado normalizado de Mesas.estado.
   * @param {object|null} cuenta Cuenta abierta de la mesa.
   * @returns {string} libre, ocupada, cerrando o inactiva.
   */
  function estadoEfectivo(guardado, cuenta) {
    if (guardado === "inactiva") return "inactiva";
    if (!cuenta || num(cuenta.total) <= 0) return "libre";
    return guardado === "cerrando" ? "cerrando" : "ocupada";
  }

  /**
   * Corrige en la base las mesas cuyo estado guardado no coincide con su cuenta
   * (por ejemplo, "Ocupada" sin consumo). Solo pasa a "Libre"; nunca ocupa una mesa.
   * @param {Array<object>} mesas Resultado de cargarMesas().
   */
  async function sincronizarEstadosMesas(mesas) {
    for (const m of mesas.filter((x) => x.desincronizada && x.estado === "libre")) {
      try { await cambiarEstadoMesa(m, ESTADO.mesaLibre); m.desincronizada = false; } catch (e) { /* se reintenta en la próxima carga */ }
    }
  }

  /**
   * Detalle de la cuenta abierta de una mesa: pedidos, líneas y comprobantes emitidos.
   * @returns {Promise<{cuenta:object, pedidos:Array, items:Array, comprobantes:Array}>}
   */
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

  /** Recalcula y guarda el total de la cuenta (con IVA). */
  async function sincronizarCuenta(detalle) {
    const c = detalle.cuenta;
    const t = totales(detalle.items);
    await Api.put("api/Cuentas/PutCuenta", {
      idCuenta: c.idCuenta, idMesa: c.idMesa, estado: c.estado,
      fechaApertura: c.fechaApertura, fechaCierre: c.fechaCierre || null, total: t.total
    });
    c.total = t.total;
  }

  /** Abre la cuenta y el pedido de la mesa si aún no existen. */
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

  /**
   * Agrega una unidad de un producto a la mesa. El stock se descuenta primero;
   * si el pedido no se registra, la unidad se devuelve al inventario.
   * @returns {Promise<object>} Detalle actualizado.
   */
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
      try { await moverStock(producto.idProducto, 1, `Reversión: no se registró el pedido de Mesa ${mesa.numero}`); } catch (_) {}
      throw e;
    }
  }

  /** Cambia en ±1 la cantidad de una línea; al llegar a cero la línea se elimina. */
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
    await liberarSiVacia(mesa, detalle);
    return detalle;
  }

  /** Elimina una línea y devuelve su cantidad al inventario. */
  async function quitarItem(mesa, detalle, idDetalle) {
    const item = detalle.items.find((i) => i.idDetalle === idDetalle);
    if (!item) throw falla(404, "La línea ya no existe.");
    await Api.del("api/DetallePedidos/DeleteDetallePedido/" + idDetalle);
    detalle.items = detalle.items.filter((i) => i.idDetalle !== idDetalle);
    await moverStock(item.idProducto, item.cantidad, `Devolución desde Mesa ${mesa.numero}`);
    await sincronizarCuenta(detalle);
    await liberarSiVacia(mesa, detalle);
    return detalle;
  }

  /** Una mesa que se queda sin productos vuelve a "Libre". La cuenta abierta se conserva y se reutiliza. */
  async function liberarSiVacia(mesa, detalle) {
    if (!detalle.items.length && mesa.estado !== "libre") await cambiarEstadoMesa(mesa, ESTADO.mesaLibre);
  }

  /** Cierra la cuenta para cobro: la mesa pasa a "Cerrando". */
  async function confirmarPago(mesa, detalle) {
    if (!detalle.items.length) throw falla(400, "La mesa no tiene productos para cobrar.");
    await sincronizarCuenta(detalle);
    await cambiarEstadoMesa(mesa, ESTADO.mesaCerrando);
  }

  async function reabrirCuenta(mesa) {
    await cambiarEstadoMesa(mesa, ESTADO.mesaOcupada);
  }

  /**
   * Registra el pago: Comprobante → Cuenta cerrada (con fecha de cierre) → Pedidos pagados → Mesa libre.
   * Es reintentable: si el comprobante ya existe, no se duplica.
   * @param {number} idMetodo Método con el que pagó el cliente.
   * @returns {Promise<{idComprobante:number|null, fechaApertura:string, fechaCierre:string, subtotal:number, iva:number, total:number}>}
   */
  async function cobrar(mesa, detalle, idMetodo) {
    if (!detalle.items.length || !detalle.cuenta) throw falla(400, "La mesa no tiene una cuenta con productos para cobrar.");
    if (!idMetodo) throw falla(400, "Seleccione un método de pago.");
    const t = totales(detalle.items);
    const c = detalle.cuenta;
    const fechaCierre = ahora();

    if (!detalle.comprobantes.length) {
      const comp = datos(await Api.post("api/Comprobantes/PostComprobante", {
        idCuenta: c.idCuenta, fecha: ahora(), total: t.total, idMetodo
      }));
      detalle.comprobantes.push(comp || {});
      Api.auditar("Comprobantes", "INSERT", null, { idCuenta: c.idCuenta, numeroMesa: mesa.numero, total: t.total, idMetodo });
    }

    await Api.put("api/Cuentas/PutCuenta", {
      idCuenta: c.idCuenta, idMesa: c.idMesa, estado: ESTADO.cuentaCerrada,
      fechaApertura: c.fechaApertura, fechaCierre, total: t.total
    });
    for (const p of detalle.pedidos) {
      await Api.put("api/Pedidos/PutPedidos", {
        idPedido: p.idPedido, idCuenta: p.idCuenta, idUsuario: p.idUsuario, fecha: p.fecha, estadoPedido: ESTADO.pedidoPagado
      });
    }
    await cambiarEstadoMesa(mesa, ESTADO.mesaLibre);
    Api.auditar("Cuentas", "UPDATE", { idCuenta: c.idCuenta, estado: ESTADO.cuentaAbierta }, { idCuenta: c.idCuenta, estado: ESTADO.cuentaCerrada, total: t.total });
    mesa.cuenta = null;
    const comp = detalle.comprobantes[0] || {};
    return {
      idComprobante: comp.idComprobante || null,
      fechaApertura: c.fechaApertura,
      fechaCierre,
      subtotal: t.subtotal, iva: t.iva, total: t.total
    };
  }

  /**
   * Retira una mesa del salón (estado "Inactiva") sin borrarla. Se usa cuando la mesa tiene cuentas
   * asociadas: la base no permite eliminarla para conservar el historial de ventas.
   */
  async function retirarMesa(mesa) {
    await cambiarEstadoMesa(mesa, ESTADO.mesaInactiva);
  }

  async function reactivarMesa(mesa) {
    await cambiarEstadoMesa(mesa, ESTADO.mesaLibre);
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

  /* ==================== Historial de órdenes ==================== */

  /** Identificador visible de una orden: ORD-000123 (se basa en idCuenta). */
  const idOrden = (idCuenta) => "ORD-" + String(idCuenta).padStart(6, "0");

  /**
   * Órdenes pagadas (cuentas con comprobante), agrupando los pedidos de cada cuenta.
   * @returns {Promise<Array<object>>} Ordenadas de la más reciente a la más antigua.
   */
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
          idUsuario: p.idUsuario,
          fechaHora: comp.fecha || c.fechaCierre || p.fecha,
          apertura: c.fechaApertura, cierre: c.fechaCierre || comp.fecha,
          idComprobante: comp.idComprobante || null,
          total: c.comprobantes.reduce((a, x) => a + num(x.total), 0),
          metodoPago: (comp.metodoPago && comp.metodoPago.nombreMetodo) || "—",
          idMetodo: comp.idMetodo || (comp.metodoPago && comp.metodoPago.idMetodo) || null,
          estado: "Pagada", idPedidos: [], items: []
        };
        porCuenta.set(c.idCuenta, o);
      }
      o.idPedidos.push(p.idPedido);
    });
    return [...porCuenta.values()].sort((a, b) => String(b.fechaHora).localeCompare(String(a.fechaHora)));
  }

  /**
   * Órdenes pagadas con sus líneas (producto, categoría, cantidad y precio), subtotal e IVA.
   * Base de los filtros y reportes de Historiales.
   * @returns {Promise<{ordenes:Array<object>, productos:Array<object>}>}
   */
  async function ordenesConItems() {
    const [ordenes, detalles, prods] = await Promise.all([
      ordenesPagadas(),
      Api.lista("api/DetallePedidos/GetDetallePedidos"),
      productos()
    ]);
    const mapa = new Map(prods.map((p) => [p.idProducto, p]));
    const porPedido = new Map();
    detalles.forEach((d) => {
      if (!porPedido.has(d.idPedido)) porPedido.set(d.idPedido, []);
      porPedido.get(d.idPedido).push(d);
    });
    ordenes.forEach((o) => {
      o.items = [];
      o.idPedidos.forEach((idp) => (porPedido.get(idp) || []).forEach((d) => {
        const p = mapa.get(d.idProducto) || {};
        o.items.push({
          idProducto: d.idProducto, nombre: p.nombre || `Producto #${d.idProducto}`,
          categoria: p.categoria || "Sin categoría",
          cantidad: num(d.cantidad), precioUnitario: num(d.precioUnitario)
        });
      }));
      const t = totales(o.items);
      o.subtotal = t.subtotal;
      o.iva = t.iva;
    });
    return { ordenes, productos: prods };
  }

  /** Líneas de una orden con el nombre de cada producto. */
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

  /* ==================== Compras ==================== */

  /** Identificador visible de una compra: CMP-000123. */
  const idCompraTexto = (id) => "CMP-" + String(id).padStart(6, "0");

  /** Compras con proveedor y líneas de detalle. Costos en COP, sin IVA. */
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
          idProducto: d.idProducto,
          nombre: p.nombre || `Producto #${d.idProducto}`, categoria: p.categoria || "—",
          cantidad: num(d.cantidad), costoUnitario: num(d.precioCompra)
        };
      });
      return {
        id: idCompraTexto(c.idCompra), idCompra: c.idCompra,
        fecha: c.fecha, idProveedor: c.idProveedor, proveedor: nomProv.get(c.idProveedor) || `Proveedor #${c.idProveedor}`,
        items, costoTotal: num(c.total)
      };
    }).sort((a, b) => b.idCompra - a.idCompra);
  }

  /**
   * Registra una compra con sus líneas y la entrada de stock de cada producto.
   * Si falla alguna línea, se eliminan las ya creadas para no dejar la compra incompleta.
   * @param {{idProveedor:number, fecha:string|null, items:Array<{idProducto:number, cantidad:number, costoUnitario:number}>}} datos
   *   fecha: "AAAA-MM-DD" o null para usar el momento actual.
   * @returns {Promise<{compra:object, aviso:string[]}>}
   */
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

  /* ==================== Analíticas ==================== */

  /** Indicadores de venta del período (Analiticas/Ventas). */
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

  /** Consumo por producto y por categoría de los pedidos pagados del período, antes de IVA. */
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

  /** API pública de la capa de negocio. */
  window.Negocio = {
    IVA, ESTADO, norm, totales, claveEstadoMesa,
    productos, categorias, proveedores, metodosPago, crearCategoria, actualizarCategoria, eliminarCategoria, crearMetodoPago,
    ajustarStock, crearProducto, actualizarProducto, eliminarProducto,
    cargarMesas, sincronizarEstadosMesas, cargarDetalle, agregarProducto, cambiarCantidad, quitarItem,
    confirmarPago, reabrirCuenta, cobrar, crearMesa, eliminarMesa, retirarMesa, reactivarMesa,
    ordenesPagadas, ordenesConItems, itemsOrden, listarCompras, crearCompra,
    ventas, consumoPeriodo
  };
})();
