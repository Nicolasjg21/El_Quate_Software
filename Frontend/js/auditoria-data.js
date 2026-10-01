/* ==========================================================================
   AUDITORIA-DATA.JS — Capa de consulta de Auditoría (El Cuate)
   --------------------------------------------------------------------------
   Auditoría es SOLO CONSULTA: los registros los genera el Backend.
     apiAuditoria.listar()  ->  GET api/Auditorias/GetAuditorias
                                (requiere el permiso "auditorias.consultar")
   El Backend entrega { idAuditoria, tabla, accion, idUsuario, fecha,
   datosAnteriores, datosNuevos }; aquí se adapta a la forma que usa la
   pantalla: { idAuditoria, fecha "AAAA-MM-DDTHH:mm", usuario, tabla, modulo,
   accion, detalle, anteriores{}, nuevos{} }.
   ========================================================================== */

const AUDITORIA_ACCIONES = ["INSERT", "UPDATE", "DELETE", "LOGIN", "LOGOUT"];

const pad2 = (n) => String(n).padStart(2, "0");

/** DateTime del Backend -> "AAAA-MM-DDTHH:mm". Si trae zona (Z / +hh:mm) se pasa a hora local. */
function fechaAuditoria(valor) {
  if (!valor) return "1970-01-01T00:00";
  const texto = String(valor);
  if (/(Z|[+-]\d{2}:?\d{2})$/.test(texto)) {
    const d = new Date(texto);
    if (!isNaN(d)) return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}T${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
  }
  return texto.slice(0, 16);
}

/** datosAnteriores / datosNuevos (texto, normalmente JSON) -> pares etiqueta → valor. */
function datosAPares(texto) {
  if (texto === null || texto === undefined || texto === "") return undefined;
  try {
    const obj = JSON.parse(texto);
    if (obj && typeof obj === "object" && !Array.isArray(obj)) {
      const salida = {};
      Object.keys(obj).forEach(k => {
        const v = obj[k];
        salida[k] = (v !== null && typeof v === "object") ? JSON.stringify(v) : String(v);
      });
      return salida;
    }
  } catch (e) { /* no es JSON: se muestra como texto */ }
  return { "Datos": String(texto) };
}

function adaptarAuditoria(a, nombresUsuarios) {
  const accion = String(a.accion || "").toUpperCase();
  const tabla = a.tabla || "—";
  return {
    idAuditoria: a.idAuditoria,
    fecha: fechaAuditoria(a.fecha),
    usuario: nombresUsuarios[a.idUsuario] || `Usuario #${a.idUsuario}`,
    tabla,
    modulo: tabla,
    accion,
    detalle: `${accion} en ${tabla}`,
    anteriores: datosAPares(a.datosAnteriores),
    nuevos: datosAPares(a.datosNuevos)
  };
}

const apiAuditoria = {
  listar() {
    return Promise.all([
      Api.lista("api/Auditorias/GetAuditorias"),
      /* Los nombres son opcionales: si falla la consulta se muestra "Usuario #id". */
      Api.lista("api/Usuarios/GetUsuarios").catch(() => [])
    ]).then(([auditorias, usuarios]) => {
      const nombres = {};
      usuarios.forEach(u => { nombres[u.idUsuario] = `${u.nombres} ${u.apellidos}`.trim(); });
      return auditorias.map(a => adaptarAuditoria(a, nombres));
    });
  }
};
