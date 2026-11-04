/**
 * @file historial-data.js
 * @description Utilidades de fecha, búsqueda y períodos de tiempo del módulo Historiales.
 *   La semana inicia el lunes (convención en Colombia).
 */

function histPad(n) { return n.toString().padStart(2, "0"); }

/** Fecha local "AAAA-MM-DD". */
function histFechaISO(fecha) {
  return `${fecha.getFullYear()}-${histPad(fecha.getMonth() + 1)}-${histPad(fecha.getDate())}`;
}

/** Fecha local "DD/MM/AAAA". */
function histFormatoFecha(fecha) {
  return `${histPad(fecha.getDate())}/${histPad(fecha.getMonth() + 1)}/${fecha.getFullYear()}`;
}

/** Fecha y hora local "DD/MM/AAAA HH:mm". */
function histFormatoFechaHora(fecha) {
  return `${histFormatoFecha(fecha)} ${histPad(fecha.getHours())}:${histPad(fecha.getMinutes())}`;
}

/** Lunes a las 00:00 de la semana que contiene la fecha. */
function histInicioSemana(fecha) {
  const f = new Date(fecha);
  const dia = f.getDay();
  const diff = (dia === 0 ? 6 : dia - 1);
  f.setDate(f.getDate() - diff);
  f.setHours(0, 0, 0, 0);
  return f;
}

/** Comparadores de calendario respecto a "ahora". */
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

/** Texto en minúsculas y sin tildes, para búsquedas. */
function histNormalizar(texto) {
  return (texto === undefined || texto === null ? "" : String(texto))
    .toString()
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}

/* ---------- Períodos ---------- */

/** Etiquetas de los períodos disponibles en los filtros. */
const HIST_PERIODOS = {
  hoy: "Hoy", ayer: "Ayer", "7dias": "Últimos 7 días", semana: "Esta semana",
  "30dias": "Últimos 30 días", mes: "Este mes", mesAnterior: "Mes anterior",
  anio: "Este año", todos: "Todo el historial", rango: "Rango personalizado"
};

/**
 * Rango de fechas de un período. Ambos extremos son inclusivos; null indica sin límite.
 * @param {string} clave Clave de HIST_PERIODOS.
 * @param {string} [desdeTxt] "AAAA-MM-DD", solo para el rango personalizado.
 * @param {string} [hastaTxt] "AAAA-MM-DD", solo para el rango personalizado.
 * @param {Date} [ahora] Fecha de referencia; por defecto, la actual.
 * @returns {{desde: Date|null, hasta: Date|null}}
 */
function histRangoPeriodo(clave, desdeTxt, hastaTxt, ahora) {
  const hoy = new Date(ahora || new Date()); hoy.setHours(0, 0, 0, 0);
  const finDe = (d) => { const f = new Date(d); f.setHours(23, 59, 59, 999); return f; };
  const diasAtras = (n) => { const d = new Date(hoy); d.setDate(d.getDate() - n); return d; };
  const desdeISO = (t) => { if (!t) return null; const [a, m, d] = t.split("-").map(Number); return new Date(a, m - 1, d, 0, 0, 0, 0); };
  switch (clave) {
    case "hoy": return { desde: hoy, hasta: finDe(hoy) };
    case "ayer": return { desde: diasAtras(1), hasta: finDe(diasAtras(1)) };
    case "7dias": return { desde: diasAtras(6), hasta: finDe(hoy) };
    case "semana": return { desde: histInicioSemana(hoy), hasta: finDe(hoy) };
    case "30dias": return { desde: diasAtras(29), hasta: finDe(hoy) };
    case "mes": return { desde: new Date(hoy.getFullYear(), hoy.getMonth(), 1), hasta: finDe(hoy) };
    case "mesAnterior": {
      const ini = new Date(hoy.getFullYear(), hoy.getMonth() - 1, 1);
      const fin = new Date(hoy.getFullYear(), hoy.getMonth(), 0);
      return { desde: ini, hasta: finDe(fin) };
    }
    case "anio": return { desde: new Date(hoy.getFullYear(), 0, 1), hasta: finDe(hoy) };
    case "rango": {
      const d = desdeISO(desdeTxt), h = desdeISO(hastaTxt);
      return { desde: d, hasta: h ? finDe(h) : null };
    }
    default: return { desde: null, hasta: null };
  }
}

/** Descripción del período para resúmenes y nombres de archivo. */
function histEtiquetaPeriodo(clave, rango) {
  if (clave !== "rango") return HIST_PERIODOS[clave] || "Todo el historial";
  const d = rango.desde ? histFormatoFecha(rango.desde) : "el inicio";
  const h = rango.hasta ? histFormatoFecha(rango.hasta) : "hoy";
  return `Del ${d} al ${h}`;
}
