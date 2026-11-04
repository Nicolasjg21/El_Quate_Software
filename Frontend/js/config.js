/**
 * @file config.js
 * @description Configuración del cliente. Único punto donde se define la URL base de la API.
 *   - API_URL: dirección del Backend, sin barra final (desarrollo: https://localhost:7135).
 *   - TIMEOUT_MS: tiempo máximo de espera de cada petición, en milisegundos.
 *   El origen desde el que se sirve el Frontend debe estar registrado en Cors:AllowedOrigins del Backend.
 *   Este archivo no debe contener credenciales.
 */

window.ELCUATE_CONFIG = Object.freeze({
  API_URL: "https://localhost:7135",
  TIMEOUT_MS: 20000
});
