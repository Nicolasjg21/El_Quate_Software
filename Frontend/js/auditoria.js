/**
 * @file auditoria.js
 * @description Módulo Auditoría (requiere "auditorias.consultar"). Solo consulta: búsqueda,
 *   filtros por módulo, acción, usuario y fechas, paginación y detalle con la comparación
 *   de datos anteriores y nuevos.
 * @requires auditoria-data.js, admin-ui.js, data.js, auth.js
 */

/** Registros por página. */
const POR_PAGINA = 8;

/** Estado de la pantalla. */
let registros = [];
let filtrosAplicados = { tabla: "", accion: "", usuario: "", desde: "", hasta: "" };
let textoBusqueda = "";
let paginaActual = 1;
let resultados = [];

const $ = (id) => document.getElementById(id);

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();
  conectarCierreModales();
  limpiarErrorAlEscribir($("form-filtros"));

  $("buscar-auditoria").addEventListener("input", (e) => {
    textoBusqueda = e.target.value;
    paginaActual = 1;
    calcularResultados();
    renderTabla();
  });

  $("form-filtros").addEventListener("submit", (e) => { e.preventDefault(); aplicarFiltros(); });
  $("btn-limpiar").addEventListener("click", limpiarFiltros);

  $("tabla-auditoria").addEventListener("click", (e) => {
    const ver = e.target.closest("button[data-ver]");
    if (ver) return abrirDetalle(Number(ver.dataset.ver));
    if (e.target.closest("[data-limpiar-filtros]")) return limpiarFiltros();
    if (e.target.closest("[data-reintentar]")) return cargarRegistros();
  });

  $("paginacion").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-pagina]");
    if (!btn || btn.disabled) return;
    paginaActual = Number(btn.dataset.pagina);
    renderTabla();
  });

  Auth.listo.then(cargarRegistros);
});

const pad = (n) => String(n).padStart(2, "0");

/* ---------- Formato ---------- */
function partesFecha(iso) {
  if (!iso) return { dia: "", hora: "", solofecha: "" };
  const [f, h] = iso.split("T");
  const [a, m, d] = f.split("-");
  return { dia: `${d}/${m}/${a}`, hora: h, solofecha: f };
}
function formatoFecha(iso) { if (!iso) return "Sin fecha"; const p = partesFecha(iso); return `${p.dia} ${p.hora}`; }

/** Clase de color según el código de acción. */
function claseAccion(accion) {
  return { INSERT: "accion--insert", UPDATE: "accion--update", DELETE: "accion--delete" }[accion] || "accion--acceso";
}

/* ---------- Consulta ---------- */
function cargarRegistros() {
  $("tabla-auditoria").innerHTML = filaMensaje("Cargando registros de auditoría...");
  $("conteo-registros").textContent = "Cargando...";
  $("paginacion").innerHTML = "";

  apiAuditoria.listar()
    .then(datos => {
      registros = datos.sort((a, b) => (b.fecha || "").localeCompare(a.fecha || "") || b.idAuditoria - a.idAuditoria);
      poblarFiltros();
      calcularResultados();
      renderTabla();
    })
    .catch(err => {
      $("conteo-registros").textContent = "No se pudo cargar";
      $("tabla-auditoria").innerHTML =
        filaMensaje(Api.mensajeError(err, "No fue posible cargar los registros de auditoría. Intente nuevamente."),
          `<button type="button" class="btn btn--secundario" data-reintentar>Reintentar</button>`, true);
      $("tabla-pie").textContent = "Mostrando 0 de 0 registros";
    });
}

/** Opciones de los filtros: el valor es el código técnico y el texto, el nombre natural. */
function poblarFiltros() {
  const tablas = [...new Set(registros.map(r => r.tabla))].sort((a, b) => moduloAuditoria(a).localeCompare(moduloAuditoria(b), "es"));
  const acciones = [...new Set([...AUDITORIA_ACCIONES, ...registros.map(r => r.accion)])];
  const usuarios = [...new Set(registros.map(r => r.usuario))].sort((a, b) => a.localeCompare(b, "es"));

  $("filtro-tabla").innerHTML = `<option value="">Todos</option>` + tablas.map(t => `<option value="${esc(t)}">${esc(moduloAuditoria(t))}</option>`).join("");
  $("filtro-accion").innerHTML = `<option value="">Todas</option>` + acciones.map(a => `<option value="${esc(a)}">${esc(verboAuditoria(a))}</option>`).join("");
  $("filtro-usuario").innerHTML = `<option value="">Todos</option>` + usuarios.map(u => `<option value="${esc(u)}">${esc(u)}</option>`).join("");
}

/* ---------- Búsqueda y filtros ---------- */

/** Texto indexado para la búsqueda: incluye nombres naturales, códigos y valores; los montos coinciden con o sin formato. */
function textoBuscable(r) {
  const valores = [...Object.entries(r.anteriores || {}), ...Object.entries(r.nuevos || {})]
    .flatMap(([k, v]) => [k, etiquetaCampo(k), v, String(v).replace(/[$.\s]/g, "")]);
  return normalizar([
    r.idAuditoria, "#" + r.idAuditoria, r.usuario, r.modulo, r.tabla, r.accion, r.accionTexto, r.detalle,
    formatoFecha(r.fecha), partesFecha(r.fecha).dia, ...valores
  ].join(" | "));
}

function calcularResultados() {
  const q = normalizar(textoBusqueda);
  const f = filtrosAplicados;
  resultados = registros.filter(r => {
    if (f.tabla && r.tabla !== f.tabla) return false;
    if (f.accion && r.accion !== f.accion) return false;
    if (f.usuario && r.usuario !== f.usuario) return false;
    const dia = partesFecha(r.fecha).solofecha;
    if ((f.desde || f.hasta) && !dia) return false;
    if (f.desde && dia < f.desde) return false;
    if (f.hasta && dia > f.hasta) return false;
    return !q || textoBuscable(r).includes(q);
  });
}

function hayCriterios() {
  const f = filtrosAplicados;
  return !!(normalizar(textoBusqueda) || f.tabla || f.accion || f.usuario || f.desde || f.hasta);
}

/** Aplica los filtros del formulario. Valida que la fecha inicial no sea posterior a la final. */
function aplicarFiltros() {
  limpiarErrores($("form-filtros"));
  const desde = $("filtro-desde").value;
  const hasta = $("filtro-hasta").value;
  if (desde && hasta && desde > hasta) {
    marcarError($("filtro-desde"), "La fecha inicial no puede ser posterior a la final.");
    return;
  }
  filtrosAplicados = {
    tabla: $("filtro-tabla").value,
    accion: $("filtro-accion").value,
    usuario: $("filtro-usuario").value,
    desde, hasta
  };
  paginaActual = 1;
  calcularResultados();
  renderTabla();
}

function limpiarFiltros() {
  $("form-filtros").reset();
  limpiarErrores($("form-filtros"));
  textoBusqueda = "";
  filtrosAplicados = { tabla: "", accion: "", usuario: "", desde: "", hasta: "" };
  paginaActual = 1;
  calcularResultados();
  renderTabla();
}

/* ---------- Tabla y paginación ---------- */
function filaMensaje(texto, botonHTML, error) {
  const boton = botonHTML ? `<div>${botonHTML}</div>` : "";
  return `<tr class="fila-mensaje"><td colspan="7" class="estado-tabla${error ? " estado-tabla--error" : ""}"><p>${esc(texto)}</p>${boton}</td></tr>`;
}

function renderTabla() {
  const cuerpo = $("tabla-auditoria");
  const total = resultados.length;
  const paginas = Math.max(1, Math.ceil(total / POR_PAGINA));
  if (paginaActual > paginas) paginaActual = paginas;

  if (registros.length === 0) {
    cuerpo.innerHTML = filaMensaje("No hay registros de auditoría disponibles.");
  } else if (total === 0) {
    cuerpo.innerHTML = filaMensaje("No se encontraron registros de auditoría.",
      hayCriterios() ? `<button type="button" class="btn btn--secundario" data-limpiar-filtros>Limpiar filtros</button>` : "");
  } else {
    const ini = (paginaActual - 1) * POR_PAGINA;
    cuerpo.innerHTML = resultados.slice(ini, ini + POR_PAGINA).map(filaRegistro).join("");
  }

  $("conteo-registros").textContent = `${total} ${total === 1 ? "registro encontrado" : "registros encontrados"}`;
  renderPie(total, paginas);
}

function filaRegistro(r) {
  return `
    <tr data-id="${r.idAuditoria}">
      <td class="col-id">#${esc(r.idAuditoria)}</td>
      <td class="col-fecha">${esc(formatoFecha(r.fecha))}</td>
      <td class="col-nombre">${esc(r.usuario)}</td>
      <td>${esc(r.modulo)}</td>
      <td><span class="accion ${claseAccion(r.accion)}">${esc(r.accionTexto)}</span></td>
      <td class="col-detalle">${esc(r.detalle)}</td>
      <td class="col-centro">
        <button type="button" class="btn-icono" data-ver="${r.idAuditoria}" aria-label="Ver detalle" title="Ver detalle">
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/></svg>
        </button>
      </td>
    </tr>`;
}

function renderPie(total, paginas) {
  const ini = total === 0 ? 0 : (paginaActual - 1) * POR_PAGINA + 1;
  const fin = Math.min(paginaActual * POR_PAGINA, total);
  $("tabla-pie").textContent = total === 0
    ? `Mostrando 0 de ${registros.length} registros`
    : `Mostrando ${ini}-${fin} de ${total} registros`;

  const nav = $("paginacion");
  if (total === 0 || paginas <= 1) { nav.innerHTML = ""; return; }

  let html = `<button type="button" class="pag-btn" data-pagina="${paginaActual - 1}" ${paginaActual === 1 ? "disabled" : ""}>Anterior</button>`;
  for (let p = 1; p <= paginas; p++) {
    html += `<button type="button" class="pag-btn${p === paginaActual ? " pag-btn--activo" : ""}" data-pagina="${p}" ${p === paginaActual ? 'aria-current="page"' : ""} aria-label="Página ${p}">${p}</button>`;
  }
  html += `<button type="button" class="pag-btn" data-pagina="${paginaActual + 1}" ${paginaActual === paginas ? "disabled" : ""}>Siguiente</button>`;
  nav.innerHTML = html;
}

/* ---------- Detalle ---------- */

/** Bloque de datos del detalle. Con "contraste", resalta los campos que cambiaron. */
function bloqueDatos(titulo, tipo, datos, contraste) {
  const filas = Object.entries(datos).map(([k, v]) => {
    const cambio = contraste && contraste[k] !== v;
    return `<div class="dato-fila${cambio ? " dato-fila--cambio" : ""}"><span class="dato-etiqueta">${esc(etiquetaCampo(k))}</span><span class="dato-valor">${esc(valorCampo(k, v))}</span></div>`;
  }).join("");
  return `<div class="bloque-datos bloque-datos--${tipo}"><p class="bloque-datos-titulo">${esc(titulo)}</p><div class="datos-lista">${filas}</div></div>`;
}

/** Muestra el detalle. En una modificación se comparan los datos anteriores con los nuevos. */
function abrirDetalle(id) {
  const r = registros.find(x => x.idAuditoria === id);
  if (!r) return;

  $("det-id").textContent = "#" + r.idAuditoria;
  $("det-fecha").textContent = formatoFecha(r.fecha);
  $("det-usuario").textContent = `${r.usuario} (ID ${r.idUsuario})`;
  $("det-tabla").textContent = r.tabla;
  $("det-modulo").textContent = r.modulo;
  $("det-accion").innerHTML = `<span class="accion ${claseAccion(r.accion)}">${esc(r.accionTexto)}</span>`;
  $("det-descripcion").textContent = r.detalle;

  const ant = r.anteriores, nue = r.nuevos;
  let html = "";
  if (r.accion === "LOGIN" || r.accion === "LOGOUT") {
    html = `<div class="comparacion comparacion--simple">${bloqueDatos(r.accion === "LOGIN" ? "Información del acceso" : "Información de la salida", "nuevo", nue || {})}</div>`;
  } else if (ant && nue) {
    const resaltar = r.accion === "UPDATE";
    html = `<div class="comparacion">${bloqueDatos("Datos anteriores", "anterior", ant, resaltar && nue)}${bloqueDatos("Datos nuevos", "nuevo", nue, resaltar && ant)}</div>`;
  } else if (ant || nue) {
    html = `<div class="comparacion comparacion--simple">${ant ? bloqueDatos("Datos anteriores", "anterior", ant) : bloqueDatos("Datos nuevos", "nuevo", nue)}</div>`;
  } else {
    html = `<p class="estado-tabla">Este registro no tiene datos anteriores ni nuevos.</p>`;
  }
  $("det-datos").innerHTML = html;

  abrirModal("overlay-detalle");
}
