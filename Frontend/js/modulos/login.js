/* ==========================================================================
   LOGIN
   --------------------------------------------------------------------------
   POST /api/Autenticador/Login  { email, password }  →  { token }
   Con el token se identifica al usuario (idUsuario/nombre) y se entra al panel.
   ========================================================================== */
(function (App) {
  "use strict";

  var ui = App.ui;

  document.addEventListener("DOMContentLoaded", function () {
    // Si ya hay una sesión vigente, no se vuelve a pedir login.
    if (App.sesion.token() && !App.sesion.expirada()) {
      window.location.replace("html/inicio.html");
      return;
    }

    var motivo = new URLSearchParams(window.location.search).get("motivo");
    if (motivo === "expirada") ui.aviso("Tu sesión expiró. Inicia sesión de nuevo.", "error");

    ui.poner("apiUrl", App.config.API_URL);
    document.getElementById("formLogin").addEventListener("submit", entrar);
    document.getElementById("botonGuardarUrl").addEventListener("click", function () {
      App.config.cambiarApiUrl(ui.valor("apiUrl"));
      ui.aviso("URL guardada. Recargando…", "exito");
      setTimeout(function () { window.location.reload(); }, 500);
    });
  });

  async function entrar(ev) {
    ev.preventDefault();

    var r = ui.validar([
      { id: "email", etiqueta: "El correo", requerido: true, tipo: "email" },
      { id: "password", etiqueta: "La contraseña", requerido: true }
    ]);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    var boton = document.getElementById("botonEntrar");
    await ui.conBoton(boton, async function () {
      boton.textContent = "Entrando…";
      try {
        // El campo de contraseña NO se recorta (los espacios pueden ser parte de la clave).
        var token = await App.api.login(r.valores.email, document.getElementById("password").value);
        await App.sesion.iniciar(token);
        window.location.href = "html/inicio.html";
      } catch (e) {
        ui.aviso(e.status === 401 ? (e.message || "Usuario o contraseña incorrectos.") : e.message, "error");
      } finally {
        boton.textContent = "Entrar";
      }
    });
  }
})(window.App = window.App || {});
