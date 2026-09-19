/* ==========================================================================
   AUDITORÍA
   --------------------------------------------------------------------------
   Un GET trae todos los registros; el resto (buscar, filtrar por módulo,
   usuario y fechas, y paginar) se hace en el navegador sobre esa lista.
   Si el backend no responde, se ven datos de ejemplo.
   ========================================================================== */

var registrosTodos = [];
var filtrosAplicados = { texto: "", modulo: "Todas", usuario: "Todos", desde: "", hasta: "" };
var paginaActual = 1;
var POR_PAGINA = 8;

var auditoriaEjemplo = [
  { id: "AUD-001", fecha: "15/09/2026 10:32", usuario: "Nicolás Velandia", modulo: "Productos", accion: "UPDATE", detalle: "Actualización de producto" },
  { id: "AUD-002", fecha: "15/09/2026 10:15", usuario: "Andrés Cardona",   modulo: "Compras",   accion: "INSERT", detalle: "Creación de compra" },
  { id: "AUD-003", fecha: "15/09/2026 09:58", usuario: "Juan Camilo Restrepo", modulo: "Pedidos", accion: "UPDATE", detalle: "Actualización de pedido" },
  { id: "AUD-004", fecha: "15/09/2026 08:05", usuario: "Daniela Vélez",    modulo: "Usuarios",  accion: "LOGIN",  detalle: "Inicio de sesión" }
];

$(document).ready(function () {
    protegerPagina();
    $("#botonSalir").click(cerrarSesion);

    $("#botonFiltrar").click(aplicarFiltros);
    $("#botonLimpiar").click(function () {
        $("#filtroTexto").val(""); $("#filtroModulo").val("Todas"); $("#filtroUsuario").val("Todos");
        $("#filtroDesde").val(""); $("#filtroHasta").val("");
        aplicarFiltros();
    });

    listarAuditoria();
});


function listarAuditoria() {
    $.ajax({
        url: URL_AUDITORIAS, type: "GET", contentType: "application/json; charset=utf-8",
        success: function (r) { registrosTodos = r; prepararFiltros(); aplicarFiltros(); },
        error: function () { registrosTodos = auditoriaEjemplo; prepararFiltros(); aplicarFiltros(); }
    });
}


function prepararFiltros() {
    var modulos = ["Todas"], usuarios = ["Todos"];
    registrosTodos.forEach(function (r) {
        if (modulos.indexOf(r.modulo) === -1) modulos.push(r.modulo);
        if (usuarios.indexOf(r.usuario) === -1) usuarios.push(r.usuario);
    });
    $("#filtroModulo").html(modulos.map(function (m) { return "<option>" + m + "</option>"; }).join(""));
    $("#filtroUsuario").html(usuarios.map(function (u) { return "<option>" + u + "</option>"; }).join(""));
}


function aplicarFiltros() {
    filtrosAplicados = {
        texto: $("#filtroTexto").val().toLowerCase(),
        modulo: $("#filtroModulo").val() || "Todas",
        usuario: $("#filtroUsuario").val() || "Todos",
        desde: $("#filtroDesde").val(),
        hasta: $("#filtroHasta").val()
    };
    paginaActual = 1;
    dibujarTabla();
}


function registrosFiltrados() {
    return registrosTodos.filter(function (r) {
        var texto = !filtrosAplicados.texto ||
            [r.usuario, r.modulo, r.accion, r.detalle].some(function (v) {
                return String(v).toLowerCase().indexOf(filtrosAplicados.texto) !== -1;
            });
        var modulo = filtrosAplicados.modulo === "Todas" || r.modulo === filtrosAplicados.modulo;
        var usuario = filtrosAplicados.usuario === "Todos" || r.usuario === filtrosAplicados.usuario;
        return texto && modulo && usuario;
    });
}


function colorAccion(accion) {
    return { "INSERT": "gris", "UPDATE": "naranja", "DELETE": "rojo", "LOGIN": "oscuro", "LOGOUT": "oscuro" }[accion] || "gris";
}


function dibujarTabla() {
    var filtrados = registrosFiltrados();
    var totalPaginas = Math.max(1, Math.ceil(filtrados.length / POR_PAGINA));
    if (paginaActual > totalPaginas) paginaActual = totalPaginas;

    var visibles = filtrados.slice((paginaActual - 1) * POR_PAGINA, paginaActual * POR_PAGINA);

    $("#conteoAuditoria").text("Registros de auditoría — " + filtrados.length + (filtrados.length === 1 ? " registro" : " registros"));

    var filas = visibles.map(function (r) {
        return "<tr>" +
            "<td class='texto-xs texto-suave'>" + r.fecha + "</td>" +
            "<td>" + r.usuario + "</td>" +
            "<td>" + r.modulo + "</td>" +
            "<td><span class='badge " + colorAccion(r.accion) + "'>" + r.accion + "</span></td>" +
            "<td>" + r.detalle + "</td>" +
        "</tr>";
    }).join("");

    $("#cuerpoTabla").html(filas || "<tr><td colspan='5' class='centro'>No se encontraron registros</td></tr>");

    dibujarPaginacion(totalPaginas, filtrados.length);
}


function dibujarPaginacion(totalPaginas, totalRegistros) {
    var desde = totalRegistros === 0 ? 0 : (paginaActual - 1) * POR_PAGINA + 1;
    var hasta = Math.min(paginaActual * POR_PAGINA, totalRegistros);
    $("#rangoPagina").text("Mostrando " + desde + "-" + hasta + " de " + totalRegistros);

    var html = "<button id='pagAnterior'" + (paginaActual === 1 ? " disabled" : "") + ">Anterior</button>";
    for (var i = 1; i <= totalPaginas; i++) {
        html += "<button class='pagina" + (i === paginaActual ? " activo" : "") + "' data-pagina='" + i + "'>" + i + "</button>";
    }
    html += "<button id='pagSiguiente'" + (paginaActual === totalPaginas ? " disabled" : "") + ">Siguiente</button>";
    $("#botonesPagina").html(html);

    $("#pagAnterior").click(function () { paginaActual--; dibujarTabla(); });
    $("#pagSiguiente").click(function () { paginaActual++; dibujarTabla(); });
    $(".pagina").click(function () { paginaActual = Number($(this).data("pagina")); dibujarTabla(); });
}
