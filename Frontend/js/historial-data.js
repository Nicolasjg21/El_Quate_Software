/* ==========================================================================
   HISTORIAL-DATA.JS — Utilidades de fecha y búsqueda del módulo Historiales
   --------------------------------------------------------------------------
   Solo contiene funciones de formato / comparación de fechas. Los datos de
   órdenes y compras vienen de la API (ver api-negocio.js); aquí ya no existe
   ningún almacenamiento local ni datos de demostración.
   ========================================================================== */

/* ---------- Utilidades de fecha ---------- */
function histPad(n) { return n.toString().padStart(2, "0"); }

function histFechaISO(fecha) {
  return `${fecha.getFullYear()}-${histPad(fecha.getMonth() + 1)}-${histPad(fecha.getDate())}`;
}

function histFormatoFecha(fecha) {
  return `${histPad(fecha.getDate())}/${histPad(fecha.getMonth() + 1)}/${fecha.getFullYear()}`;
}

function histFormatoFechaHora(fecha) {
  return `${histFormatoFecha(fecha)} ${histPad(fecha.getHours())}:${histPad(fecha.getMinutes())}`;
}

/** Lunes 00:00 de la semana que contiene "fecha" (convención colombiana: semana inicia lunes). */
function histInicioSemana(fecha) {
  const f = new Date(fecha);
  const dia = f.getDay(); // 0 = domingo, 1 = lunes ... 6 = sábado
  const diff = (dia === 0 ? 6 : dia - 1); // días desde el lunes
  f.setDate(f.getDate() - diff);
  f.setHours(0, 0, 0, 0);
  return f;
}

function histEsHoy(fecha, ahora) {
  return fecha.getFullYear() === ahora.getFullYear() &&
    fecha.getMonth() === ahora.getMonth() &&
    fecha.getDate() === ahora.getDate();
}

function histEsEstaSemana(fecha, ahora) {
  const inicio = histInicioSemana(ahora);
  const finDia = new Date(ahora); finDia.setHours(23, 59, 59, 999);
  return fecha >= inicio && fecha <= finDia;
}

function histEsEsteMes(fecha, ahora) {
  return fecha.getFullYear() === ahora.getFullYear() && fecha.getMonth() === ahora.getMonth();
}

/* ---------- Normalización de texto para búsqueda (minúsculas + sin tildes) ---------- */
function histNormalizar(texto) {
  return (texto === undefined || texto === null ? "" : String(texto))
    .toString()
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}
