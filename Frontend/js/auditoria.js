/* ==========================================================================
   AUDITORIA.JS — Consulta, filtros, búsqueda, paginación y detalle
   Módulo de solo lectura: no crea, edita ni elimina registros.
   ========================================================================== */

const POR_PAGINA = 8;

let registros = [];                // resultado del GET
let filtrosAplicados = { tabla: "", accion: "", usuario: "", desde: "", hasta: "" };
let textoBusqueda = "";
let paginaActual = 1;
let resultados = [];               // registros que cumplen búsqueda + filtros

const $ = (id) => document.getElementById(id);

document.addEventListener("DOMContentLoaded", () => {
  $("fecha-actual").textContent = fechaHoy();
  conectarCierreModales();
  limpiarErrorAlEscribir($("form-filtros"));

  /* Búsqueda en vivo (se combina con los filtros ya aplicados) */
  $("buscar-auditoria").addEventListener("input", (e) => {
    textoBusqueda = e.target.value;
    paginaActual = 1;
    calcularResultados();
    renderTabla();
  });

  /* Filtrar (botón o Enter) */
  $("form-filtros").addEventListener("submit", (e) => { e.preventDefault(); aplicarFiltros(); });
  $("btn-limpiar").addEventListener("click", limpiarFiltros);

  /* Delegación: ojo (detalle) y botones dentro de la tabla */
  $("tabla-auditoria").addEventListener("click", (e) => {
    const ver = e.target.closest("button[data-ver]");
    if (ver) return abrirDetalle(Number(ver.dataset.ver));
    if (e.target.closest("[data-limpiar-filtros]")) return limpiarFiltros();
    if (e.target.closest("[data-reintentar]")) return cargarRegistros();
  });

  /* Paginación */
  $("paginacion").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-pagina]");
    if (!btn || btn.disabled) return;
    paginaActual = Number(btn.dataset.pagina);
    renderTabla();
  });

  Auth.listo.then(cargarRegistros);
});

/* ---------- Formato ---------- */
const pad = (n) => String(n).padStart(2, "0");

function partesFecha(iso) {           // "2026-09-23T14:32"
  const [f, h] = iso.split("T");
  const [a, m, d] = f.split("-");
  return { dia: `${d}/${m}/${a}`, hora: h, solofecha: f };
}
function formatoFecha(iso) { const p = partesFecha(iso); return `${p.dia} ${p.hora}`; }

function claseAccion(accion) {
  return { INSERT: "accion--insert", UPDATE: "accion--update", DELETE: "accion--delete" }[accion] || "accion--acceso";
}

/* ---------- GET ---------- */
function cargarRegistros() {
  $("tabla-auditoria").innerHTML = filaMensaje("Cargando registros de auditoría...");
  $("conteo-registros").textContent = "Cargando...";
  $("paginacion").innerHTML = "";

  apiAuditoria.listar()
    .then(datos => {
      registros = datos.sort((a, b) => b.fecha.localeCompare(a.fecha));
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

function poblarFiltros() {
  const tablas = [...new Set(registros.map(r => r.tabla))].sort((a, b) => a.localeCompare(b, "es"));
  const acciones = [...new Set([...AUDITORIA_ACCIONES, ...registros.map(r => r.accion)])];
  const usuarios = [...new Set(registros.map(r => r.usuario))].sort((a, b) => a.localeCompare(b, "es"));

  $("filtro-tabla").innerHTML = `<option value="">Todas</option>` + tablas.map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join("");
  $("filtro-accion").innerHTML = `<option value="">Todas</option>` + acciones.map(a => `<option value="${esc(a)}">${esc(a)}</option>`).join("");
  $("filtro-usuario").innerHTML = `<option value="">Todos</option>` + usuarios.map(u => `<option value="${esc(u)}">${esc(u)}</option>`).join("");
}

/* ---------- Búsqueda + filtros ---------- */
function textoBuscable(r) {
  const valores = [...Object.entries(r.anteriores || {}), ...Object.entries(r.nuevos || {})]
    .flatMap(([k, v]) => [k, v, String(v).replace(/[$.\s]/g, "")]);   // "$ 5.500" también coincide con "5500"
  return normalizar([
    r.idAuditoria, r.usuario, r.modulo, r.tabla, r.accion, r.detalle,
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
    const dia = partesFecha(r.fecha).solofecha;            // AAAA-MM-DD (comparación inclusiva)
    if (f.desde && dia < f.desde) return false;
    if (f.hasta && dia > f.hasta) return false;
    return !q || textoBuscable(r).includes(q);
  });
}

function hayCriterios() {
  const f = filtrosAplicados;
  return !!(normalizar(textoBusqueda) || f.tabla || f.accion || f.usuario || f.desde || f.hasta);
}

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

/* ---------- Render ---------- */
function filaMensaje(texto, botonHTML, error) {
  const boton = botonHTML ? `<div>${botonHTML}</div>` : "";
  return `<tr class="fila-mensaje"><td colspan="6" class="estado-tabla${error ? " estado-tabla--error" : ""}"><p>${esc(texto)}</p>${boton}</td></tr>`;
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
      <td class="col-fecha">${formatoFecha(r.fecha)}</td>
      <td class="col-nombre">${esc(r.usuario)}</td>
      <td>${esc(r.modulo)}</td>
      <td><span class="accion ${claseAccion(r.accion)}">${esc(r.accion)}</span></td>
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
function bloqueDatos(titulo, tipo, datos, contraste) {
  const filas = Object.entries(datos).map(([k, v]) => {
    const cambio = contraste && contraste[k] !== v;      // resalta lo que cambió en UPDATE
    return `<div class="dato-fila${cambio ? " dato-fila--cambio" : ""}"><span class="dato-etiqueta">${esc(k)}</span><span class="dato-valor">${esc(v)}</span></div>`;
  }).join("");
  return `<div class="bloque-datos bloque-datos--${tipo}"><p class="bloque-datos-titulo">${esc(titulo)}</p><div class="datos-lista">${filas}</div></div>`;
}

function abrirDetalle(id) {
  const r = registros.find(x => x.idAuditoria === id);
  if (!r) return;

  $("det-id").textContent = r.idAuditoria;
  $("det-fecha").textContent = formatoFecha(r.fecha);
  $("det-usuario").textContent = r.usuario;
  $("det-tabla").textContent = r.tabla;
  $("det-modulo").textContent = r.modulo;
  $("det-accion").innerHTML = `<span class="accion ${claseAccion(r.accion)}">${esc(r.accion)}</span>`;
  $("det-descripcion").textContent = r.detalle;

  const ant = r.anteriores, nue = r.nuevos;
  let html = "";
  if (r.accion === "UPDATE" && ant && nue) {
    html = `<div class="comparacion">${bloqueDatos("Datos anteriores", "anterior", ant, nue)}${bloqueDatos("Datos nuevos", "nuevo", nue, ant)}</div>`;
  } else if (r.accion === "DELETE" && ant) {
    html = `<div class="comparacion comparacion--simple">${bloqueDatos("Datos anteriores", "anterior", ant)}</div>`;
  } else if (r.accion === "LOGIN" || r.accion === "LOGOUT") {
    html = `<div class="comparacion comparacion--simple">${bloqueDatos(r.accion === "LOGIN" ? "Información del acceso" : "Información de la salida", "nuevo", nue || {})}</div>`;
  } else if (nue) {
    html = `<div class="comparacion comparacion--simple">${bloqueDatos("Datos nuevos", "nuevo", nue)}</div>`;
  }
  $("det-datos").innerHTML = html;

  abrirModal("overlay-detalle");
}
