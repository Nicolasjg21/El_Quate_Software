/**
 * @file config.js
 * @description Configuración del cliente. Único punto donde se define la URL base de la API.
 *   - API_URL: dirección del Backend, sin barra final (desarrollo: https://localhost:7135).
 *   - TIMEOUT_MS: tiempo máximo de espera de cada petición, en milisegundos.
 *   - TIMEOUT_LOGIN_MS: espera del inicio de sesión. Es mayor porque suele ser la primera
 *     petición del día y la base de datos de Azure puede estar reanudándose (hasta ~1 minuto).
 *   - REINTENTOS_LECTURA: reintentos automáticos de una consulta (GET) cuando no hubo respuesta.
 *   - AVISO_LENTO_MS: tras este tiempo sin respuesta se muestra el aviso "Conectando con el servidor".
 *   - SOPORTE: datos de contacto que muestra la página de Ayuda (responsable, correo, teléfono y horario).
 *   - VERSION: versión del sistema que se muestra en la Ayuda y viaja en cada reporte de problema.
 *   - MANUAL_URL: ruta del manual de usuario en PDF. Si el archivo no existe, la Ayuda oculta el enlace.
 *     Son datos públicos del bar; complételos antes de la entrega.
 *   El origen desde el que se sirve el Frontend debe estar registrado en Cors:AllowedOrigins del Backend.
 *   Este archivo no debe contener credenciales.
 */

window.ELCUATE_CONFIG = Object.freeze({
  API_URL: "https://localhost:7135",
  TIMEOUT_MS: 45000,
  TIMEOUT_LOGIN_MS: 90000,
  REINTENTOS_LECTURA: 2,
  AVISO_LENTO_MS: 4000,
  VERSION: "1.0.0",
  MANUAL_URL: "../docs/manual-usuario.pdf",
  SOPORTE: Object.freeze({
    responsable: "Administración de El Cuate",
    correo: "soportetecnicoelquate@gmail.com",
    telefono: "",
    horario: "Martes a domingo, de 2:00 p. m. a 3:00 a. m."
  })
});
