/* ==========================================================================
   CATÁLOGOS — CRUD genérico de las tablas simples
   categorias, proveedores, metodospago, roles, permisos, tipoDocumento
   --------------------------------------------------------------------------
   Una sola pantalla para seis tablas: cada catálogo se describe con sus
   campos y el formulario, la tabla y las validaciones se generan solos.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, R = App.config.REGLAS;

  var CATALOGOS = {
    categorias:    { titulo: "Categorías",   tabla: "categorias",  campos: [
                       { id: "nombreCategoria", etiqueta: "Nombre de la categoría", requerido: true, largoMax: R.NOMBRE_MAX }] },
    proveedores:   { titulo: "Proveedores",  tabla: "proveedores", campos: [
                       { id: "nombreProveedor", etiqueta: "Nombre del proveedor", requerido: true, largoMax: 100 },
                       { id: "telefono", etiqueta: "Teléfono", requerido: true, tipo: "telefono", largoMax: R.PROVEEDOR_TEL_MAX },
                       { id: "direccion", etiqueta: "Dirección", requerido: true, largoMax: R.PROVEEDOR_DIR_MAX }] },
    metodosPago:   { titulo: "Métodos de pago", tabla: "metodospago", campos: [
                       { id: "nombreMetodo", etiqueta: "Nombre del método", requerido: true, largoMax: R.METODO_MAX }] },
    roles:         { titulo: "Roles",        tabla: "roles",       campos: [
                       { id: "nombreRol", etiqueta: "Nombre del rol", requerido: true, largoMax: R.ROL_MAX }] },
    permisos:      { titulo: "Permisos",     tabla: "permisos",    campos: [
                       { id: "nombrePermiso", etiqueta: "Nombre del permiso", requerido: true, largoMax: R.PERMISO_MAX }] },
    tipoDocumento: { titulo: "Tipos de documento", tabla: "tipoDocumento", campos: [
                       { id: "nombreTipo", etiqueta: "Nombre del tipo", requerido: true, largoMax: 50 }] }
  };

  var pedido = new URLSearchParams(window.location.search).get("c");   // ?c=metodosPago abre esa pestaña
  var actual = CATALOGOS[pedido] ? pedido : "categorias", registros = [], busqueda = "";

  App.pagina(async function () {
    ui.html("pestanasCatalogo", Object.keys(CATALOGOS).map(function (k) {
      return "<button data-catalogo='" + k + "'" + (k === actual ? " class='activa'" : "") + ">" + ui.esc(CATALOGOS[k].titulo) + "</button>";
    }).join(""));

    ui.alHacerClic("pestanasCatalogo", "button[data-catalogo]", function (b) {
      actual = b.dataset.catalogo;
      busqueda = "";
      ui.poner("busquedaCatalogo", "");
      ui.$$("#pestanasCatalogo button").forEach(function (x) { x.classList.toggle("activa", x.dataset.catalogo === actual); });
      cerrarFormulario();
      cargar().catch(mostrarError);
    });

    document.getElementById("botonMostrarForm").addEventListener("click", function () { abrirFormulario(null); });
    document.getElementById("botonCancelar").addEventListener("click", cerrarFormulario);
    document.getElementById("botonGuardar").addEventListener("click", guardar);
    document.getElementById("busquedaCatalogo").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintar();
    }, 200));

    ui.alHacerClic("cuerpoTabla", "button[data-accion]", function (b) {
      var id = Number(b.dataset.id);
      if (b.dataset.accion === "editar") abrirFormulario(registros.filter(function (r) { return r[pk()] === id; })[0]);
      if (b.dataset.accion === "eliminar") eliminar(id, b);
    });

    await cargar();
  });

  function def() { return CATALOGOS[actual]; }
  function pk() { return App.api.entidad(actual).pk; }
  function mostrarError(e) { ui.aviso(App.api.explicar(e), "error"); }

  async function cargar() {
    registros = await App.api.entidad(actual).listar();
    ui.html("cabezaTabla", "<tr><th style='width:80px'>#</th>" +
      def().campos.map(function (c) { return "<th>" + ui.esc(c.etiqueta) + "</th>"; }).join("") +
      "<th class='centro' style='width:180px'>Acciones</th></tr>");
    pintar();
  }

  function pintar() {
    var visibles = registros.filter(function (r) {
      if (!busqueda) return true;
      return def().campos.some(function (c) { return String(r[c.id] || "").toLowerCase().indexOf(busqueda) !== -1; });
    });
    var filas = visibles.map(function (r) {
      return "<tr><td class='texto-suave'>" + r[pk()] + "</td>" +
        def().campos.map(function (c) { return "<td>" + ui.esc(r[c.id]) + "</td>"; }).join("") +
        "<td class='centro'><div class='acciones' style='justify-content:center'>" +
          "<button class='pequeno' data-accion='editar' data-id='" + r[pk()] + "'>Editar</button>" +
          "<button class='pequeno rojo' data-accion='eliminar' data-id='" + r[pk()] + "'>Eliminar</button>" +
        "</div></td></tr>";
    });
    ui.html("cuerpoTabla", ui.filasOVacio(filas, def().campos.length + 2, "No hay registros en " + def().titulo + "."));
  }

  function abrirFormulario(registro) {
    ui.mostrar("formCatalogo", true);
    ui.poner("idRegistro", registro ? registro[pk()] : "");
    ui.html("camposCatalogo", def().campos.map(function (c) {
      var valor = registro ? ui.esc(registro[c.id]) : "";
      return "<div class='campo'><label for='" + c.id + "'>" + ui.esc(c.etiqueta) + (c.requerido ? " *" : "") + "</label>" +
             "<input type='text' id='" + c.id + "' value='" + valor + "'" +
             (c.largoMax ? " maxlength='" + c.largoMax + "'" : "") + "></div>";
    }).join(""));
    ui.texto("tituloFormulario", registro ? "Editando registro #" + registro[pk()] : "Nuevo registro en " + def().titulo);
    window.scrollTo(0, 0);
  }

  function cerrarFormulario() { ui.mostrar("formCatalogo", false); }

  async function guardar() {
    var r = ui.validar(def().campos);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var id = ui.valor("idRegistro");
    var cuerpo = {};
    def().campos.forEach(function (c) { cuerpo[c.id] = r.valores[c.id]; });

    var repetido = registros.filter(function (x) {
      var primero = def().campos[0].id;
      return String(x[pk()]) !== String(id) &&
             String(x[primero]).trim().toLowerCase() === String(cuerpo[primero]).trim().toLowerCase();
    })[0];
    if (repetido) { ui.aviso("Ya existe un registro con ese nombre.", "error"); return; }

    await ui.conBoton(document.getElementById("botonGuardar"), async function () {
      try {
        if (id) {
          cuerpo[pk()] = Number(id);
          var antes = registros.filter(function (x) { return x[pk()] === Number(id); })[0];
          await App.api.entidad(actual).editar(cuerpo);
          await App.api.auditar(def().tabla, "UPDATE", antes, cuerpo);
          ui.aviso("Registro actualizado correctamente.", "exito");
        } else {
          await App.api.entidad(actual).crear(cuerpo);
          await App.api.auditar(def().tabla, "INSERT", null, cuerpo);
          ui.aviso("Registro creado correctamente.", "exito");
        }
        cerrarFormulario();
        await cargar();
      } catch (e) { mostrarError(e); }
    });
  }

  async function eliminar(id, boton) {
    var registro = registros.filter(function (r) { return r[pk()] === id; })[0];
    if (!confirm("¿Eliminar este registro de " + def().titulo + "?\n\nSi hay filas que lo referencian (productos, usuarios, compras…), la base de datos no permitirá borrarlo.")) return;
    await ui.conBoton(boton, async function () {
      try {
        await App.api.entidad(actual).eliminar(id);
        await App.api.auditar(def().tabla, "DELETE", registro, null);
        ui.aviso("Registro eliminado.", "exito");
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e, "eliminar"), "error"); }
    });
  }
})(window.App = window.App || {});
