/* ==========================================================================
   LOGIN
   --------------------------------------------------------------------------
   Manda usuario y contraseña al backend. Si responde bien, guarda el token
   y entra al sistema.
   ========================================================================== */

   

$(document).ready(function () {

    $("#botonEntrar").click(entrar);

    // Permite entrar con Enter en vez de tener que hacer clic.
    $("#password").keypress(function (evento) {
        if (evento.which === 13) {
            entrar();
        }
    });
});


function entrar() {

    var email = $("#email").val();
    var password = $("#password").val();

    if (email === "" || password === "") {
        mostrarMensaje("Escribe tu usuario y tu contraseña", "error");
        return;
    }

    $.ajax({
        url: URL_LOGIN,
        type: "POST",
        contentType: "application/json; charset=utf-8",

        /* ⚠ Confirma qué nombres espera tu Controller.
           Si espera "usuario" en vez de "email", cámbialo aquí. */
        data: JSON.stringify({
            email: email,
            password: password
        }),

        success: function (response) {

            /* ⚠ Confirma cómo se llama la propiedad del token en la
               respuesta real. Si el backend devuelve "Token" con
               mayúscula, cambia response.token por response.Token */
            guardarSesion(response.token, response.nombre || email);
            window.location.href = "inicio.html";
        },

        error: function (xhr) {
            if (xhr.status === 401) {
                mostrarMensaje("Usuario o contraseña incorrectos", "error");
            } else if (xhr.status === 0) {
                mostrarMensaje("No hay conexión con el servidor. ¿Está corriendo el backend?", "error");
            } else {
                mostrarMensaje("Error al iniciar sesión (código " + xhr.status + ")", "error");
            }
        }
    });
}


function mostrarMensaje(texto, tipo) {
    $("#mensaje").text(texto);
    $("#mensaje").attr("class", "mensaje " + tipo);
}
