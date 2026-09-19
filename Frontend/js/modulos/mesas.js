/* ==========================================================================
   MESAS
   --------------------------------------------------------------------------
   Lista las mesas (GET), registra una nueva (POST) y, al hacer clic en una
   mesa, manda a la pantalla de su cuenta (cuenta.html?id=...).

   Si el backend todavía no responde, se usan datos de ejemplo
   (mesasEjemplo) para que la pantalla se vea completa mientras conectas
   la API real. En cuanto listarMesas() reciba una respuesta del backend,
   se usa esa respuesta y ya no los datos de ejemplo.
   ========================================================================== */

var mesas = [];
var filtroActual = "todas";

/* Datos de ejemplo — se ven en pantalla SÓLO si el backend no responde. */
var mesasEjemplo = [
  { id: 1, numero: "1", estado: "Libre",   personas: 0, total: 0,      hora: "" },
  { id: 2, numero: "2", estado: "Ocupada", personas: 4, total: 127500, hora: "14:32" },
  { id: 3, numero: "3", estado: "Ocupada", personas: 2, total: 89250,  hora: "14:15" },
  { id: 4, numero: "4", estado: "Cerrando",personas: 3, total: 156500, hora: "13:20" },
  { id: 5, numero: "5", estado: "Libre",   personas: 0, total: 0,      hora: "" },
  { id: 6, numero: "6", estado: "Ocupada", personas: 2, total: 234800, hora: "14:50" }
];

$(document).ready(function () {
    protegerPagina();

    $("#botonSalir").click(cerrarSesion);
    $("#botonNuevaMesa").click(function () { $("#formNuevaMesa").removeClass("oculto"); });
    $("#botonCancelarMesa").click(function () { $("#formNuevaMesa").addClass("oculto"); });
    $("#botonRegistrar").click(registrarMesa);

    $(".chip").click(function () {
        filtroActual = $(this).data("filtro");
        $(".chip").removeClass("activo");
        $(this).addClass("activo");
        dibujarMesas();
    });

    listarMesas();
});


/* ---------- GET: listar ---------- */
function listarMesas() {

    $.ajax({
        url: URL_MESAS,
        type: "GET",
        contentType: "application/json; charset=utf-8",

        success: function (respuesta) {
            mesas = respuesta;
            dibujarMesas();
        },

        error: function (xhr) {
            // Sin backend todavía: se muestran los datos de ejemplo.
            mesas = mesasEjemplo;
            dibujarMesas();
        }
    });
}


/* ---------- Resumen de estados (cajas de color) ---------- */
function dibujarResumen() {
    var ocupadas = mesas.filter(function (m) { return m.estado === "Ocupada"; }).length;
    var libres   = mesas.filter(function (m) { return m.estado === "Libre"; }).length;
    var cerrando = mesas.filter(function (m) { return m.estado === "Cerrando"; }).length;
    var total = mesas.length;

    $("#resumenMesas").html(
        '<div class="estado-caja roja">' +
          '<div class="etiqueta"><span class="punto rojo"></span> Ocupadas</div>' +
          '<div class="valor">' + ocupadas + ' <small>/ ' + total + '</small></div>' +
        '</div>' +
        '<div class="estado-caja verde">' +
          '<div class="etiqueta"><span class="punto verde"></span> Libres</div>' +
          '<div class="valor">' + libres + ' <small>/ ' + total + '</small></div>' +
        '</div>' +
        '<div class="estado-caja amarilla">' +
          '<div class="etiqueta"><span class="punto amarillo"></span> Cerrando</div>' +
          '<div class="valor">' + cerrando + ' <small>/ ' + total + '</small></div>' +
        '</div>'
    );
}


/* ---------- Rejilla de mesas, con color según el estado ---------- */
function dibujarMesas() {
    dibujarResumen();

    var visibles = filtroActual === "todas"
        ? mesas
        : mesas.filter(function (m) { return m.estado === filtroActual; });

    var html = "";

    for (var i = 0; i < visibles.length; i++) {
        var m = visibles[i];
        var clase = m.estado === "Ocupada" ? "ocupada" : "libre";
        var punto = m.estado === "Ocupada" ? "rojo" : (m.estado === "Cerrando" ? "amarillo" : "verde");

        var cuerpo;
        if (m.estado === "Libre") {
            cuerpo = '<div class="badge verde" style="margin-top:10px">Libre</div>';
        } else {
            cuerpo =
                '<div class="texto-xs texto-suave" style="margin-top:8px">🕐 ' + m.hora + '</div>' +
                '<div class="texto-xs texto-suave">👥 ' + m.personas + ' personas</div>' +
                '<div style="margin-top:8px;padding-top:8px;border-top:1px solid rgba(0,0,0,.08)">' +
                    '<strong>$ ' + formatoMiles(m.total) + '</strong>' +
                    '<div class="texto-xs texto-suave">' + (m.estado === "Cerrando" ? "Cerrando cuenta" : "Total actual") + '</div>' +
                '</div>';
        }

        html += '<button class="mesa-tarjeta ' + clase + '" data-id="' + m.id + '">' +
                    '<div class="fila-entre" style="display:flex;justify-content:space-between;align-items:center">' +
                        '<span class="numero">Mesa ' + m.numero + '</span>' +
                        '<span class="punto ' + punto + '"></span>' +
                    '</div>' +
                    cuerpo +
                '</button>';
    }

    if (visibles.length === 0) {
        html = '<p class="texto-suave">No hay mesas con ese estado.</p>';
    }

    $("#rejillaMesas").html(html);

    $(".mesa-tarjeta").click(function () {
        window.location.href = "cuenta.html?id=" + $(this).data("id");
    });
}


/* ---------- POST: registrar mesa nueva ---------- */
function registrarMesa() {
    var numero = $("#numeroMesa").val();

    if (numero === "") {
        mostrarMensaje("Escribe el número de la mesa", "error");
        return;
    }

    var mesaNueva = { numero: numero, estado: "Libre" };

    $.ajax({
        url: URL_MESAS,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(mesaNueva),

        success: function () {
            mostrarMensaje("Mesa registrada correctamente", "exito");
            $("#numeroMesa").val("");
            $("#formNuevaMesa").addClass("oculto");
            listarMesas();
        },

        error: function (xhr) {
            mostrarMensaje("No se pudo registrar la mesa. Error " + xhr.status, "error");
        }
    });
}


function formatoMiles(n) {
    return new Intl.NumberFormat("es-CO").format(n || 0);
}

function mostrarMensaje(texto, tipo) {
    $("#mensaje").text(texto);
    $("#mensaje").attr("class", "mensaje " + tipo);
}
