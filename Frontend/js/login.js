/* ==========================================================================
   LOGIN.JS — Inicio de sesión (POST api/Autenticador/Login)
   Validaciones según LoginDTO: email obligatorio, válido y <= 254;
   password obligatoria y <= 128.
   ========================================================================== */
document.addEventListener("DOMContentLoaded", () => {
  const form = document.getElementById("form-login");
  const aviso = document.getElementById("login-error");
  const btn = document.getElementById("btn-login");
  const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  function mostrar(mensaje) { aviso.textContent = mensaje; aviso.hidden = !mensaje; }

  const motivo = new URLSearchParams(location.search).get("motivo");
  if (motivo === "expirada") mostrar("Su sesión expiró. Inicie sesión nuevamente.");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    if (btn.disabled) return;

    const email = document.getElementById("usuario").value.trim();
    const password = document.getElementById("password").value;
    const recordar = document.getElementById("recordar").checked;

    if (!email) return mostrar("El correo electrónico es obligatorio.");
    if (email.length > 254) return mostrar("El correo electrónico no puede superar 254 caracteres.");
    if (!REGEX_EMAIL.test(email)) return mostrar("Correo electrónico no válido.");
    if (!password) return mostrar("La contraseña es obligatoria.");
    if (password.length > 128) return mostrar("La contraseña no puede superar 128 caracteres.");

    mostrar("");
    btn.disabled = true;
    try {
      await Auth.login(email, password, recordar);
      location.replace("dashboard.html");
    } catch (err) {
      mostrar(Api.mensajeError(err, "No fue posible iniciar sesión."));
      document.getElementById("password").value = "";
    } finally {
      btn.disabled = false;
    }
  });
});
