/* ==========================================================================
   CONFIG.JS — Configuración del Frontend (única fuente de la URL de la API)
   --------------------------------------------------------------------------
   Para cambiar de entorno basta editar API_URL (sin barra final):
     Desarrollo : https://localhost:7135   (perfil "https" del Backend)
                  http://localhost:5079    (perfil "http")
     Producción : https://api.tu-dominio.com

   El origen desde donde se sirve este Frontend debe estar incluido en
   "Cors:AllowedOrigins" del Backend (p. ej. http://localhost:5500).
   Este archivo NO debe contener credenciales ni secretos.
   ========================================================================== */
window.ELCUATE_CONFIG = Object.freeze({
  API_URL: "https://localhost:7135",
  TIMEOUT_MS: 20000
});
