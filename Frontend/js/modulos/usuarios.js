/* ==========================================================================
   USUARIOS
   --------------------------------------------------------------------------
   Campos reales: idUsuario, nombres, apellidos, documento, idTipoDocumento,
   telefono, passwordHash, estado(bool), idRol, email.
   El backend hashea passwordHash en PostUsuarios/PutUsuarios; el frontend
   envía la contraseña EN CLARO en ese campo (y vacío = no cambiarla al editar).
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;
  var svc = App.api.entidad("usuarios");

  var usuarios = [], roles = [], tipos = [], busqueda = "", filtroRol = "todos";
  var CAMPOS = ["nombres", "apellidos", "idTipoDocumento", "documento", "telefono", "email", "idRol", "password"];

  App.pagina(async function () {
    document.getElementById("botonMostrarForm").addEventListener("click", function () { abrirFormulario(null); });
    document.getElementById("botonCancelar").addEventListener("click", cerrarFormulario);
    document.getElementById("botonGuardar").addEventListener("click", guardar);
    document.getElementById("busquedaUsuario").addEventListener("input", ui.debounce(function (e) {
      busqueda = e.target.value.trim().toLowerCase(); pintar();
    }, 200));
    document.getElementById("filtroRol").addEventListener("change", function (e) { filtroRol = e.target.value; pintar(); });
    ui.alHacerClic("cuerpoTabla", "button[data-accion]", function (b) {
      var id = Number(b.dataset.id);
      if (b.dataset.accion === "editar") editar(id);
      if (b.dataset.accion === "eliminar") eliminar(id, b);
    });
    await cargar();
  });

  async function cargar() {
    var d = await App.datos.cargar(["usuarios", "roles", "tipoDocumento"]);
    usuarios = d.usuarios; roles = d.roles; tipos = d.tipoDocumento;
    ui.html("filtroRol", "<option value='todos'>Todos los roles</option>" + ui.opciones(roles, "idRol", "nombreRol", filtroRol));
    ui.html("idRol", ui.opciones(roles, "idRol", "nombreRol", null, "— Selecciona un rol —"));
    ui.html("idTipoDocumento", ui.opciones(tipos, "idTipoDocumento", "nombreTipo", null, "— Tipo —"));
    if (!roles.length || !tipos.length) ui.aviso("Faltan roles o tipos de documento. Créalos en Catálogos antes de registrar usuarios.", "advertencia");
    pintar();
  }

  function nombreRol(id) { var r = roles.filter(function (x) { return x.idRol === id; })[0]; return r ? r.nombreRol : "(sin rol)"; }
  function nombreTipo(id) { var t = tipos.filter(function (x) { return x.idTipoDocumento === id; })[0]; return t ? t.nombreTipo : ""; }
  function colorRol(nombre) {
    var n = String(nombre).toLowerCase();
    if (n.indexOf("admin") !== -1) return "morado";
    if (n.indexOf("caj") !== -1) return "azul";
    if (n.indexOf("mesero") !== -1 || n.indexOf("mesera") !== -1) return "verde";
    return "gris";
  }

  function pintar() {
    var visibles = usuarios.filter(function (u) {
      var t = !busqueda || (ui.nombreCompleto(u) + " " + u.documento + " " + u.email).toLowerCase().indexOf(busqueda) !== -1;
      var r = filtroRol === "todos" || String(u.idRol) === String(filtroRol);
      return t && r;
    });

    var filas = visibles.map(function (u) {
      return "<tr><td>" + ui.esc(ui.nombreCompleto(u)) + "</td>" +
        "<td>" + ui.esc(u.documento) + "<div class='texto-xs texto-suave'>" + ui.esc(nombreTipo(u.idTipoDocumento)) + "</div></td>" +
        "<td>" + ui.esc(u.email) + "</td><td>" + ui.esc(u.telefono) + "</td>" +
        "<td>" + ui.badge(nombreRol(u.idRol), colorRol(nombreRol(u.idRol))) + "</td>" +
        "<td>" + ui.badge(u.estado ? "Activo" : "Inactivo", u.estado ? "verde" : "gris") + "</td>" +
        "<td class='centro'><div class='acciones' style='justify-content:center'>" +
          "<button class='pequeno' data-accion='editar' data-id='" + u.idUsuario + "'>Editar</button>" +
          "<button class='pequeno rojo' data-accion='eliminar' data-id='" + u.idUsuario + "'>Eliminar</button>" +
        "</div></td></tr>";
    });

    ui.html("cuerpoTabla", ui.filasOVacio(filas, 7, usuarios.length ? "No se encontraron usuarios con esos filtros." : "Todavía no hay usuarios registrados.", { icono: "👥" }));
    ui.texto("conteoUsuarios", "Mostrando " + visibles.length + " de " + usuarios.length + " usuarios.");
  }

  function abrirFormulario(u) {
    ui.limpiarErrores(CAMPOS);
    ui.mostrar("formUsuario", true);
    ui.poner("idUsuario", u ? u.idUsuario : "");
    ui.poner("nombres", u ? u.nombres : "");
    ui.poner("apellidos", u ? u.apellidos : "");
    ui.poner("idTipoDocumento", u ? u.idTipoDocumento : "");
    ui.poner("documento", u ? u.documento : "");
    ui.poner("telefono", u ? u.telefono : "");
    ui.poner("email", u ? u.email : "");
    ui.poner("idRol", u ? u.idRol : "");
    ui.poner("password", "");
    document.getElementById("estadoUsuario").checked = u ? !!u.estado : true;
    ui.texto("etiquetaPassword", u ? "Contraseña (vacío = no cambiar)" : "Contraseña *");
    ui.texto("tituloFormulario", u ? "Editando: " + ui.nombreCompleto(u) : "Nuevo usuario");
    window.scrollTo(0, 0);
  }
  function cerrarFormulario() { ui.mostrar("formUsuario", false); }

  async function guardar() {
    var id = ui.valor("idUsuario");
    var reglas = [
      { id: "nombres", etiqueta: "Los nombres", requerido: true, largoMin: 2, largoMax: 100 },
      { id: "apellidos", etiqueta: "Los apellidos", requerido: true, largoMin: 2, largoMax: 100 },
      { id: "idTipoDocumento", etiqueta: "El tipo de documento", requerido: true, tipo: "entero", min: 1 },
      { id: "documento", etiqueta: "El documento", requerido: true, tipo: "documento" },
      { id: "telefono", etiqueta: "El teléfono", requerido: true, tipo: "telefono" },
      { id: "email", etiqueta: "El correo", requerido: true, tipo: "email" },
      { id: "idRol", etiqueta: "El rol", requerido: true, tipo: "entero", min: 1 },
      { id: "password", etiqueta: "La contraseña", requerido: !id, largoMin: cfg.REGLAS.PASSWORD_MIN }
    ];
    var r = ui.validar(reglas);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var repetido = usuarios.filter(function (u) {
      return String(u.idUsuario) !== String(id) &&
        (String(u.email).toLowerCase() === r.valores.email.toLowerCase() || String(u.documento) === r.valores.documento);
    })[0];
    if (repetido) { ui.aviso("Ya existe otro usuario con ese correo o documento.", "error"); return; }

    var cuerpo = {
      nombres: r.valores.nombres, apellidos: r.valores.apellidos,
      documento: r.valores.documento, idTipoDocumento: r.valores.idTipoDocumento,
      telefono: r.valores.telefono, email: r.valores.email,
      idRol: r.valores.idRol, estado: document.getElementById("estadoUsuario").checked,
      passwordHash: document.getElementById("password").value   // el backend lo hashea
    };

    await ui.conBoton(document.getElementById("botonGuardar"), async function () {
      try {
        if (id) {
          cuerpo.idUsuario = Number(id);
          var antes = usuarios.filter(function (u) { return u.idUsuario === cuerpo.idUsuario; })[0];
          await svc.editar(cuerpo);
          await App.api.auditar("usuarios", "UPDATE", sinClave(antes), sinClave(cuerpo));
          ui.aviso("Usuario actualizado correctamente.", "exito");
        } else {
          await svc.crear(cuerpo);
          await App.api.auditar("usuarios", "INSERT", null, sinClave(cuerpo));
          ui.aviso("Usuario creado correctamente.", "exito");
        }
        cerrarFormulario();
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e), "error"); }
    });
  }

  function sinClave(o) {
    if (!o) return null;
    var copia = {};
    Object.keys(o).forEach(function (k) { if (k !== "passwordHash") copia[k] = o[k]; });
    return copia;
  }

  async function editar(id) {
    var u = await svc.porId(id);
    if (!u) { ui.aviso("El usuario ya no existe.", "error"); return; }
    abrirFormulario(u);
  }

  async function eliminar(id, boton) {
    var u = usuarios.filter(function (x) { return x.idUsuario === id; })[0];
    var yo = App.sesion.datos();
    if (yo && yo.idUsuario === id) { ui.aviso("No puedes eliminar el usuario con el que iniciaste sesión.", "error"); return; }
    if (!confirm("¿Eliminar a «" + ui.nombreCompleto(u) + "»?\n\nSi tiene pedidos, movimientos de kardex o auditorías, la base de datos no permitirá borrarlo; en ese caso márcalo como inactivo.")) return;
    await ui.conBoton(boton, async function () {
      try {
        await svc.eliminar(id);
        await App.api.auditar("usuarios", "DELETE", sinClave(u), null);
        ui.aviso("Usuario eliminado.", "exito");
        await cargar();
      } catch (e) { ui.aviso(App.api.explicar(e, "eliminar"), "error"); }
    });
  }
})(window.App = window.App || {});
