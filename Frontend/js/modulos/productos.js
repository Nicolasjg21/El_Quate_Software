/* ==========================================================================
   PRODUCTOS (Inventario)
   --------------------------------------------------------------------------
   Campos REALES de la tabla productos:
     idProducto, nombreProducto, precioVenta, cantidadMinima, estado(bool), idCategoria
   No existe "codigo", "precioCompra" ni "stock" en esta tabla:
     · el costo está en detalleCompras.precioCompra
     · el stock se deriva del último movimiento de kardex
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;
  var svc = App.api.entidad("productos");

  var productos = [], categorias = [], stock = new Map(), costos = new Map();
  var busqueda = "", filtroCat = "todas", filtroEstado = "todos";

  App.pagina(async function () {
    document.getElementById("botonMostrarForm").addEventListener("click", function () { abrirFormulario(null); });
    document.getElementById("botonCancelar").addEventListener("click", cerrarFormulario);
    document.getElementById("botonGuardar").addEventListener("click", guardar);
    document.getElementById("busquedaProducto").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintarTabla();
    }, 200));
    document.getElementById("filtroCategoria").addEventListener("change", function (e) { filtroCat = e.target.value; pintarTabla(); });
    document.getElementById("filtroEstado").addEventListener("change", function (e) { filtroEstado = e.target.value; pintarTabla(); });

    ui.alHacerClic("cuerpoTabla", "button[data-accion]", function (boton) {
      var id = Number(boton.dataset.id);
      if (boton.dataset.accion === "editar") editar(id);
      if (boton.dataset.accion === "eliminar") eliminar(id, boton);
    });

    ui.alHacerClic("cuerpoTabla", "button[data-nuevo]", function () { abrirFormulario(null); });

    await cargar();
  });

  async function cargar() {
    var d = await App.datos.cargar(["productos", "categorias", "kardex", "detalleCompras"]);
    productos = d.productos; categorias = d.categorias;
    stock = App.datos.stockPorProducto(d.kardex);
    costos = App.datos.ultimoCosto(d.detalleCompras);

    ui.html("filtroCategoria", "<option value='todas'>Todas las categorías</option>" +
      ui.opciones(categorias, "idCategoria", "nombreCategoria", filtroCat));
    ui.html("idCategoria", ui.opciones(categorias, "idCategoria", "nombreCategoria", null, "— Selecciona una categoría —"));
    if (!categorias.length) ui.aviso("No hay categorías registradas. Crea al menos una en Catálogos antes de registrar productos.", "advertencia");

    pintarResumen();
    pintarTabla();
  }

  function nombreCategoria(id) {
    var c = categorias.filter(function (x) { return x.idCategoria === id; })[0];
    return c ? c.nombreCategoria : "(sin categoría)";
  }
  function stockDe(p) { return stock.has(p.idProducto) ? stock.get(p.idProducto) : 0; }

  function pintarResumen() {
    var valor = productos.reduce(function (s, p) { return s + Number(p.precioVenta) * stockDe(p); }, 0);
    var bajos = productos.filter(function (p) { return stockDe(p) <= Number(p.cantidadMinima); }).length;
    var sin = productos.filter(function (p) { return stockDe(p) <= 0; }).length;
    ui.html("resumenInventario",
      caja("naranja", "Valor del inventario (precio de venta)", ui.moneda(valor)) +
      caja("amarilla", "En o bajo el mínimo", bajos) +
      caja("roja", "Sin existencias", sin));
  }
  function caja(color, etiqueta, valor) {
    return "<div class='estado-caja " + color + "'><div class='etiqueta'>" + ui.esc(etiqueta) +
           "</div><div class='valor'>" + ui.esc(valor) + "</div></div>";
  }

  function pintarTabla() {
    var visibles = productos.filter(function (p) {
      var t = !busqueda || String(p.nombreProducto).toLowerCase().indexOf(busqueda) !== -1;
      var c = filtroCat === "todas" || String(p.idCategoria) === String(filtroCat);
      var e = filtroEstado === "todos" || (filtroEstado === "activos" ? p.estado : !p.estado);
      return t && c && e;
    });

    var filas = visibles.map(function (p) {
      var s = stockDe(p), bajo = s <= Number(p.cantidadMinima);
      return "<tr" + (bajo ? " class='fila-alerta'" : "") + ">" +
        "<td>" + ui.esc(p.nombreProducto) + (costos.has(p.idProducto) ? "<div class='texto-xs texto-suave'>Último costo: " + ui.moneda(costos.get(p.idProducto)) + "</div>" : "") + "</td>" +
        "<td>" + ui.badge(nombreCategoria(p.idCategoria), "gris") + "</td>" +
        "<td class='derecha'><strong>" + ui.moneda(p.precioVenta) + "</strong></td>" +
        "<td class='derecha" + (bajo ? " dato-alerta" : "") + "'>" + (bajo ? "⚠ " : "") + ui.num(s) + barra(s, Number(p.cantidadMinima)) + "</td>" +
        "<td class='derecha'>" + ui.num(p.cantidadMinima) + "</td>" +
        "<td>" + ui.badge(p.estado ? "Activo" : "Inactivo", p.estado ? "verde" : "gris") + "</td>" +
        "<td class='centro'><div class='acciones' style='justify-content:center'>" +
          "<button class='pequeno' data-accion='editar' data-id='" + p.idProducto + "'>Editar</button>" +
          "<button class='pequeno rojo' data-accion='eliminar' data-id='" + p.idProducto + "'>Eliminar</button>" +
        "</div></td></tr>";
    });

    var sinDatos = !productos.length;
    ui.html("cuerpoTabla", ui.filasOVacio(filas, 7,
      sinDatos ? "Todavía no hay productos registrados." : "No se encontraron productos con esos filtros.",
      { icono: sinDatos ? "📦" : "🔍", accion: sinDatos ? "<button data-nuevo='1'>+ Registrar el primer producto</button>" : "" }));
    ui.texto("conteoProductos", "Mostrando " + visibles.length + " de " + productos.length + " productos.");
  }

  /* Barra de stock: rojo = sin existencias, amarillo = en/bajo el mínimo, verde = suficiente. */
  function barra(stockActual, minimo) {
    var tope = Math.max(minimo * 2, 1);
    var pct = Math.max(0, Math.min(100, Math.round((stockActual / tope) * 100)));
    var clase = stockActual <= 0 ? "critico" : (stockActual <= minimo ? "bajo" : "");
    return "<div class='barra-stock'><i class='" + clase + "' style='width:" + Math.max(pct, stockActual > 0 ? 6 : 0) + "%'></i></div>";
  }

  function abrirFormulario(p) {
    ui.limpiarErrores(["nombreProducto", "idCategoria", "precioVenta", "cantidadMinima", "stockInicial"]);
    ui.mostrar("formProducto", true);
    ui.mostrar("campoStockInicial", !p);       // el stock solo se fija al crear; después se ajusta en Kardex
    ui.poner("idProducto", p ? p.idProducto : "");
    ui.poner("nombreProducto", p ? p.nombreProducto : "");
    ui.poner("idCategoria", p ? p.idCategoria : "");
    ui.poner("precioVenta", p ? p.precioVenta : "");
    ui.poner("cantidadMinima", p ? p.cantidadMinima : "");
    ui.poner("stockInicial", 0);
    document.getElementById("estado").checked = p ? !!p.estado : true;
    ui.texto("tituloFormulario", p ? "Editando: " + p.nombreProducto : "Nuevo producto");
    window.scrollTo(0, 0);
  }
  function cerrarFormulario() { ui.mostrar("formProducto", false); }

  async function guardar() {
    var r = ui.validar([
      { id: "nombreProducto", etiqueta: "El nombre", requerido: true, largoMin: 2, largoMax: cfg.REGLAS.NOMBRE_MAX },
      { id: "idCategoria", etiqueta: "La categoría", requerido: true, tipo: "entero", min: 1 },
      { id: "precioVenta", etiqueta: "El precio de venta", requerido: true, tipo: "decimal", min: 0, max: cfg.REGLAS.PRECIO_MAX },
      { id: "cantidadMinima", etiqueta: "La cantidad mínima", requerido: true, tipo: "entero", min: 0 },
      { id: "stockInicial", etiqueta: "El stock inicial", tipo: "entero", min: 0 }
    ]);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var id = ui.valor("idProducto");
    var cuerpo = {
      nombreProducto: r.valores.nombreProducto,
      precioVenta: r.valores.precioVenta,
      cantidadMinima: r.valores.cantidadMinima,
      estado: document.getElementById("estado").checked,
      idCategoria: r.valores.idCategoria
    };

    await ui.conBoton(document.getElementById("botonGuardar"), async function () {
      try {
        if (id) {
          cuerpo.idProducto = Number(id);
          var antes = productos.filter(function (p) { return p.idProducto === cuerpo.idProducto; })[0];
          await svc.editar(cuerpo);
          await App.api.auditar("productos", "UPDATE", antes, cuerpo);
          ui.aviso("Producto actualizado correctamente.", "exito");
        } else {
          var creado = await svc.crear(cuerpo);
          await App.api.auditar("productos", "INSERT", null, creado || cuerpo);
          var inicial = Number(r.valores.stockInicial) || 0;
          if (creado && creado.idProducto && inicial > 0 && cfg.OPCIONES.KARDEX_AUTOMATICO) {
            await App.datos.moverStock({ idProducto: creado.idProducto, tipo: "ENTRADA", cantidad: inicial,
                                         stockActual: 0, motivo: "Stock inicial del producto" });
          }
          ui.aviso("Producto creado correctamente.", "exito");
        }
        cerrarFormulario();
        await cargar();
      } catch (e) {
        ui.aviso(App.api.explicar(e), "error");
      }
    });
  }

  async function editar(id) {
    var p = await svc.porId(id);
    if (!p) { ui.aviso("El producto ya no existe.", "error"); return; }
    abrirFormulario(p);
  }

  async function eliminar(id, boton) {
    var p = productos.filter(function (x) { return x.idProducto === id; })[0];
    if (!confirm("¿Eliminar el producto «" + (p ? p.nombreProducto : id) + "»?\n\nSi tiene movimientos de kardex, pedidos o compras, la base de datos no permitirá borrarlo; en ese caso márcalo como inactivo.")) return;
    await ui.conBoton(boton, async function () {
      try {
        await svc.eliminar(id);
        await App.api.auditar("productos", "DELETE", p, null);
        ui.aviso("Producto eliminado.", "exito");
        await cargar();
      } catch (e) {
        ui.aviso(App.api.explicar(e, "eliminar"), "error");
      }
    });
  }
})(window.App = window.App || {});
