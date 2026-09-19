/* ==========================================================================
   USUARIOS
   --------------------------------------------------------------------------
   Mismo patrón que productos.js: listar, guardar (crear o editar), eliminar.
   Con insignias de color por rol y datos de ejemplo si el backend no
   responde todavía.
   ========================================================================== */

var usuarios = [];
var textoBusqueda = "";
var rolFiltro = "todos";

var usuariosEjemplo = [
  { id: "USR001", nombreUsuario: "maria.garcia",    nombreCompleto: "Maria Garcia",    rol: "Administrador", registro: "2024-01-15", ultimoAcceso: "2024-05-12 14:32" },
  { id: "USR002", nombreUsuario: "carlos.rodriguez", nombreCompleto: "Carlos Rodriguez", rol: "Caja",          registro: "2024-02-10", ultimoAcceso: "2024-05-12 13:45" },
  { id: "USR003", nombreUsuario: "juan.torres",      nombreCompleto: "Juan Torres",      rol: "Mesero",        registro: "2024-03-05", ultimoAcceso: "2024-05-12 12:20" }
];

$(document).ready(function () {
    protegerPagina();
    $("#botonSalir").click(cerrarSesion);

    $("#botonMostrarForm").click(function () { abrirFormulario(null); });
    $("#botonCancelar").click(limpiarFormulario);
    $("#botonGuardar").click(guardarUsuario);

    $("#busquedaUsuario").on("input", function () { textoBusqueda = $(this).val().toLowerCase(); dibujarTabla(); });
    $("#filtroRol").change(function () { rolFiltro = $(this).val(); dibujarTabla(); });

    listarUsuarios();
});


function listarUsuarios() {
    $.ajax({
        url: URL_USUARIOS, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { usuarios = r; dibujarTabla(); },
        error: function () { usuarios = usuariosEjemplo; dibujarTabla(); }
    });
}


function colorRol(rol) {
    return { "Administrador": "morado", "Caja": "azul", "Mesero": "verde" }[rol] || "gris";
}


function dibujarTabla() {
    var visibles = usuarios.filter(function (u) {
        var texto = !textoBusqueda ||
            u.nombreCompleto.toLowerCase().indexOf(textoBusqueda) !== -1 ||
            u.nombreUsuario.toLowerCase().indexOf(textoBusqueda) !== -1;
        var rol = rolFiltro === "todos" || u.rol === rolFiltro;
        return texto && rol;
    });

    $("#conteoUsuarios").text("Mostrando " + visibles.length + " de " + usuarios.length + " usuarios");

    var filas = visibles.map(function (u) {
        return "<tr>" +
            "<td>" + u.nombreUsuario + "</td>" +
            "<td>" + u.nombreCompleto + "</td>" +
            "<td><span class='badge " + colorRol(u.rol) + "'>" + u.rol + "</span></td>" +
            "<td class='texto-xs texto-suave'>" + u.registro + "</td>" +
            "<td class='texto-xs texto-suave'>" + (u.ultimoAcceso || "—") + "</td>" +
            "<td class='centro'>" +
                "<button class='pequeno' onclick='editarUsuario(\"" + u.id + "\")'>Editar</button> " +
                "<button class='pequeno rojo' onclick='eliminarUsuario(\"" + u.id + "\")'>Eliminar</button>" +
            "</td>" +
        "</tr>";
    }).join("");

    $("#cuerpoTabla").html(filas || "<tr><td colspan='6' class='centro'>No se encontraron usuarios</td></tr>");
}


function abrirFormulario(usuario) {
    $("#formUsuario").removeClass("oculto");
    if (usuario) {
        $("#idUsuario").val(usuario.id);
        $("#nombreCompleto").val(usuario.nombreCompleto);
        $("#nombreUsuario").val(usuario.nombreUsuario);
        $("#rol").val(usuario.rol);
        $("#password").val("");
        $("#tituloFormulario").text("Editando: " + usuario.nombreCompleto);
    } else {
        limpiarCampos();
        $("#tituloFormulario").text("Nuevo usuario");
    }
    window.scrollTo(0, 0);
}


function editarUsuario(id) {
    $.ajax({
        url: URL_USUARIOS + "/" + id, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { abrirFormulario(r); },
        error: function () {
            var u = usuarios.filter(function (x) { return String(x.id) === String(id); })[0];
            if (u) abrirFormulario(u);
        }
    });
}


function guardarUsuario() {
    var id = $("#idUsuario").val();
    var datos = {
        nombreCompleto: $("#nombreCompleto").val(),
        nombreUsuario: $("#nombreUsuario").val(),
        rol: $("#rol").val(),
        password: $("#password").val()
    };

    if (datos.nombreCompleto === "" || datos.nombreUsuario === "") {
        mostrarMensaje("El nombre y el usuario son obligatorios", "error");
        return;
    }
    if (id === "" && datos.password === "") {
        mostrarMensaje("La contraseña es obligatoria para un usuario nuevo", "error");
        return;
    }

    if (id === "") {
        $.ajax({
            url: URL_USUARIOS, type: "POST", contentType: "application/json; charset=utf-8",
            data: JSON.stringify(datos),
            success: function () { mostrarMensaje("Usuario creado correctamente", "exito"); limpiarFormulario(); listarUsuarios(); },
            error: function (xhr) { mostrarMensaje("No se pudo crear. Error " + xhr.status, "error"); }
        });
    } else {
        datos.id = id;
        $.ajax({
            url: URL_USUARIOS + "/" + id, type: "PUT", contentType: "application/json; charset=utf-8",
            data: JSON.stringify(datos),
            success: function () { mostrarMensaje("Usuario actualizado correctamente", "exito"); limpiarFormulario(); listarUsuarios(); },
            error: function (xhr) { mostrarMensaje("No se pudo actualizar. Error " + xhr.status, "error"); }
        });
    }
}


function eliminarUsuario(id) {
    if (!confirm("¿Seguro que deseas eliminar este usuario?")) return;
    $.ajax({
        url: URL_USUARIOS + "/" + id, type: "DELETE", contentType: "application/json; charset=utf-8",
        success: function () { mostrarMensaje("Usuario eliminado", "exito"); listarUsuarios(); },
        error: function (xhr) { mostrarMensaje("No se pudo eliminar. Error " + xhr.status, "error"); }
    });
}


function limpiarCampos() {
    $("#idUsuario, #nombreCompleto, #nombreUsuario, #password").val("");
    $("#rol").val("Administrador");
}

function limpiarFormulario() {
    limpiarCampos();
    $("#formUsuario").addClass("oculto");
}

function mostrarMensaje(texto, tipo) {
    $("#mensaje").text(texto);
    $("#mensaje").attr("class", "mensaje " + tipo);
}
