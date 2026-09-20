/* ==========================================================================
   AUDITORÍA — auditorias(idAuditoria, tabla, accion, idUsuario, fecha,
   datosAnteriores, datosNuevos). Los nombres "modulo" y "detalle" que usaba
   la versión anterior no existen en la tabla.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui;

  var registros = [], usuarios = [], pagina = 1, POR_PAGINA = 10;
  var filtros = { texto: "", tabla: "todas", usuario: "todos", desde: "", hasta: "" };

  App.pagina(async function () {
    document.getElementById("botonFiltrar").addEventListener("click", aplicar);
    document.getElementById("botonExportar").addEventListener("click", exportar);
    document.getElementById("botonLimpiar").addEventListener("click", function () {
      ui.poner("filtroTexto", ""); ui.poner("filtroTabla", "todas"); ui.poner("filtroUsuario", "todos");
      ui.poner("filtroDesde", ""); ui.poner("filtroHasta", "");
      aplicar();
    });
    ui.alHacerClic("botonesPagina", "button[data-pagina]", function (b) {
      pagina = Number(b.dataset.pagina); pintar();
    });

    var d = await App.datos.cargar(["auditorias", "usuarios"]);
    registros = d.auditorias; usuarios = d.usuarios;

    var tablas = [];
    registros.forEach(function (r) { if (r.tabla && tablas.indexOf(r.tabla) === -1) tablas.push(r.tabla); });
    ui.html("filtroTabla", "<option value='todas'>Todas</option>" + tablas.sort().map(function (t) {
      return "<option value='" + ui.esc(t) + "'>" + ui.esc(t) + "</option>";
    }).join(""));
    ui.html("filtroUsuario", "<option value='todos'>Todos</option>" +
      ui.opciones(usuarios, "idUsuario", function (u) { return ui.nombreCompleto(u); }));

    pintar();
  });

  function nombreUsuario(id) {
    var u = usuarios.filter(function (x) { return x.idUsuario === id; })[0];
    return u ? ui.nombreCompleto(u) : "Usuario #" + id;
  }
  function colorAccion(accion) {
    var a = String(accion).toUpperCase();
    if (a.indexOf("INSERT") !== -1 || a.indexOf("CREA") !== -1) return "verde";
    if (a.indexOf("UPDATE") !== -1 || a.indexOf("ACTUALIZ") !== -1) return "naranja";
    if (a.indexOf("DELETE") !== -1 || a.indexOf("ELIMIN") !== -1) return "rojo";
    return "gris";
  }

  function aplicar() {
    filtros = {
      texto: ui.valor("filtroTexto").trim().toLowerCase(),
      tabla: ui.valor("filtroTabla") || "todas",
      usuario: ui.valor("filtroUsuario") || "todos",
      desde: ui.valor("filtroDesde"),
      hasta: ui.valor("filtroHasta")
    };
    pagina = 1;
    pintar();
  }

  function filtrados() {
    var desde = filtros.desde ? new Date(filtros.desde + "T00:00:00") : null;
    var hasta = filtros.hasta ? new Date(filtros.hasta + "T23:59:59") : null;
    return registros.filter(function (r) {
      var t = !filtros.texto || [r.tabla, r.accion, nombreUsuario(r.idUsuario)]
        .some(function (v) { return String(v).toLowerCase().indexOf(filtros.texto) !== -1; });
      var tb = filtros.tabla === "todas" || r.tabla === filtros.tabla;
      var us = filtros.usuario === "todos" || String(r.idUsuario) === String(filtros.usuario);
      var f = ui.parseFecha(r.fecha);
      var fe = (!desde || (f && f >= desde)) && (!hasta || (f && f <= hasta));
      return t && tb && us && fe;
    }).sort(function (a, b) { return b.idAuditoria - a.idAuditoria; });
  }

  function json(texto) {
    if (!texto) return "";
    try { return JSON.stringify(JSON.parse(texto), null, 2); } catch (e) { return String(texto); }
  }

  function pintar() {
    var lista = filtrados();
    var totalPaginas = Math.max(1, Math.ceil(lista.length / POR_PAGINA));
    if (pagina > totalPaginas) pagina = totalPaginas;
    var visibles = lista.slice((pagina - 1) * POR_PAGINA, pagina * POR_PAGINA);

    ui.texto("conteoAuditoria", "Registros de auditoría — " + lista.length + (lista.length === 1 ? " registro" : " registros"));

    ui.html("cuerpoTabla", ui.filasOVacio(visibles.map(function (r) {
      var antes = json(r.datosAnteriores), despues = json(r.datosNuevos);
      var detalle = "";
      if (antes || despues) {
        detalle = "<details class='datos-json'><summary>Ver cambios</summary><pre>" +
          (antes ? "ANTES:\n" + ui.esc(antes) + "\n\n" : "") +
          (despues ? "DESPUÉS:\n" + ui.esc(despues) : "") + "</pre></details>";
      } else {
        detalle = "<span class='texto-suave'>—</span>";
      }
      return "<tr><td class='texto-xs texto-suave'>" + ui.esc(ui.fecha(r.fecha)) + "</td>" +
        "<td>" + ui.esc(nombreUsuario(r.idUsuario)) + "</td>" +
        "<td>" + ui.esc(r.tabla) + "</td>" +
        "<td>" + ui.badge(r.accion, colorAccion(r.accion)) + "</td>" +
        "<td>" + detalle + "</td></tr>";
    }), 5, "No hay registros de auditoría con esos filtros."));

    var desde = lista.length ? (pagina - 1) * POR_PAGINA + 1 : 0;
    ui.texto("rangoPagina", "Mostrando " + desde + "-" + Math.min(pagina * POR_PAGINA, lista.length) + " de " + lista.length);

    var botones = "<button data-pagina='" + (pagina - 1) + "'" + (pagina === 1 ? " disabled" : "") + ">Anterior</button>";
    for (var i = 1; i <= totalPaginas; i++) {
      botones += "<button class='pagina" + (i === pagina ? " activo" : "") + "' data-pagina='" + i + "'>" + i + "</button>";
    }
    botones += "<button data-pagina='" + (pagina + 1) + "'" + (pagina === totalPaginas ? " disabled" : "") + ">Siguiente</button>";
    ui.html("botonesPagina", botones);
  }

  function exportar() {
    ui.descargarCsv("auditoria.csv", [
      { titulo: "Fecha", valor: function (r) { return ui.fecha(r.fecha); } },
      { titulo: "Usuario", valor: function (r) { return nombreUsuario(r.idUsuario); } },
      { titulo: "Tabla", valor: "tabla" },
      { titulo: "Acción", valor: "accion" },
      { titulo: "Datos anteriores", valor: function (r) { return r.datosAnteriores || ""; } },
      { titulo: "Datos nuevos", valor: function (r) { return r.datosNuevos || ""; } }
    ], filtrados());
  }
})(window.App = window.App || {});
