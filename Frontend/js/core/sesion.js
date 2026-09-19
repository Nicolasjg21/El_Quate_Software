/* ==========================================================================
   SESIÓN — recordar quién inició sesión
   --------------------------------------------------------------------------
   Única fuente de verdad para el token y el nombre del usuario. Ninguna
   otra pantalla toca localStorage directamente: todas llaman a estas
   funciones.
   ========================================================================== */

/* Guarda el token y el nombre que se van a usar en toda la aplicación. */
function guardarSesion(token, nombre) {
    localStorage.setItem("token", token);
    localStorage.setItem("nombreUsuario", nombre || "Usuario");
}

/* Lee el token guardado (o null si no hay ninguno). */
function obtenerToken() {
    return localStorage.getItem("token");
}

/* Lee el nombre guardado (o "Usuario" si no hay ninguno). */
function obtenerNombreUsuario() {
    return localStorage.getItem("nombreUsuario") || "Usuario";
}

/* Borra la sesión y regresa al login. Se usa en el botón "Cerrar sesión". */
function cerrarSesion() {
    localStorage.removeItem("token");
    localStorage.removeItem("nombreUsuario");
    window.location.href = "index.html";
}

/* Se llama al principio de cada pantalla interna (no en el login).
   Si no hay token guardado, no deja seguir: manda de vuelta al login.
   Si sí hay sesión, aprovecha y llena el encabezado (nombre, fecha, avatar). */
function protegerPagina() {
    if (!obtenerToken()) {
        window.location.href = "index.html";
        return;
    }
    mostrarUsuarioEnEncabezado();
}

/* ==========================================================================
   Arreglo del botón "atrás" del navegador después de cerrar sesión
   --------------------------------------------------------------------------
   Cuando usas las flechas de atrás/adelante, algunos navegadores restauran
   la página exactamente como se veía en pantalla, SIN volver a ejecutar el
   $(document).ready() de la página (esto se llama "bfcache"). Por eso, si
   cerrabas sesión y pulsabas "atrás", podías seguir viendo el panel aunque
   ya no hubiera token guardado.

   Este evento "pageshow" avisa cada vez que la página se muestra, incluso
   cuando viene del bfcache (evento.persisted === true). En ese caso,
   volvemos a comprobar la sesión.
   ========================================================================== */
window.addEventListener("pageshow", function (evento) {
    if (evento.persisted) {
        protegerPagina();
    }
});

/* Llena el nombre, la fecha de hoy y la inicial del avatar en el
   encabezado de la pantalla, si esos elementos existen en el HTML. */
function mostrarUsuarioEnEncabezado() {
    var nombre = obtenerNombreUsuario();

    var elNombre = document.getElementById("nombreUsuarioTop");
    if (elNombre) elNombre.textContent = nombre;

    var elFecha = document.getElementById("fechaHoy");
    if (elFecha) elFecha.textContent = new Date().toLocaleDateString("es-CO");

    var elAvatar = document.getElementById("inicialUsuario");
    if (elAvatar) elAvatar.textContent = nombre.charAt(0).toUpperCase();
}
