/* ==========================================================================
   CUENTA DE MESA
   --------------------------------------------------------------------------
   Esta es la pantalla más difícil de las nueve, porque junta tres entidades:
   Mesas, Pedidos/DetallePedidos y Comprobantes. Como no puedo ver tu backend
   real, las peticiones de aquí son las MÁS PROBABLES según el nombre de tus
   Controllers, pero son las que con más seguridad vas a tener que ajustar.

   ⚠ SUPUESTOS QUE HAY QUE CONFIRMAR:
     1. Que se puede pedir "los detalles de pedido de una mesa" agregando
        ?idMesa=X a la URL de DetallePedidos. Si tu Controller no lo soporta,
        toca traer TODOS los detalles (GET sin filtro) y filtrar aquí mismo
        con un "for", como se hace en pintarMesas() de mesas.js.
     2. Que crear un detalle de pedido nuevo es un POST a DetallePedidos con
        idMesa, idProducto y cantidad.
     3. Que registrar el pago es un POST a Comprobantes con idMesa y total.

   Si algo de esto no coincide con tu backend, es cuestión de cambiar la URL
   o el objeto que se envía — la forma de escribir el $.ajax no cambia.
   ========================================================================== */

var idMesaActual = null;
var productosDisponibles = [];   // se llena al cargar la página, para el <select>
var itemsPedido = [];            // lo que se muestra en la tabla


$(document).ready(function () {
    protegerPagina();

    $("#botonSalir").click(cerrarSesion);
    $("#botonAgregar").click(agregarProducto);
    $("#botonPagar").click(registrarPago);

    // Leemos el "?id=" de la URL: cuenta.html?id=3
    idMesaActual = new URLSearchParams(window.location.search).get("id");

    if (!idMesaActual) {
        mostrarMensaje("No se indicó qué mesa abrir", "error");
        return;
    }

    $("#tituloMesa").text("Mesa " + idMesaActual);

    cargarProductosParaSelect();
    cargarPedidoDeLaMesa();
});


/* ---------- Traer los productos, para llenar el <select> ---------- */
function cargarProductosParaSelect() {

    $.ajax({
        url: URL_PRODUCTOS,
        type: "GET",
        contentType: "application/json; charset=utf-8",

        success: function (productos) {
            productosDisponibles = productos;

            var opciones = "";
            for (var i = 0; i < productos.length; i++) {
                opciones = opciones +
                    "<option value='" + productos[i].id + "'>" +
                        productos[i].nombre + " — $" + productos[i].precioVenta +
                    "</option>";
            }
            $("#productoSeleccionado").html(opciones);
        },

        error: function (xhr) {
            mostrarMensaje("No se pudieron cargar los productos. Error " + xhr.status, "error");
        }
    });
}


/* ---------- GET: traer lo que ya lleva pedido esta mesa ---------- */
function cargarPedidoDeLaMesa() {

    $.ajax({
        // ⚠ Ver el supuesto 1 al inicio del archivo.
        url: URL_DETALLE_PEDIDOS + "?idMesa=" + idMesaActual,
        type: "GET",
        contentType: "application/json; charset=utf-8",

        success: function (detalles) {
            itemsPedido = detalles;
            pintarTablaPedido();
        },

        error: function (xhr) {
            // Si la mesa todavía no tiene pedido, es normal que dé 404.
            // No lo tratamos como error grave: mostramos la tabla vacía.
            itemsPedido = [];
            pintarTablaPedido();
        }
    });
}


/* ---------- Pintar la tabla y el total ---------- */
function pintarTablaPedido() {

    var filas = "";
    var subtotal = 0;

    for (var i = 0; i < itemsPedido.length; i++) {

        var item = itemsPedido[i];
        var importe = item.cantidad * item.precioUnitario;
        subtotal = subtotal + importe;

        filas = filas +
            "<tr>" +
                "<td>" + item.nombreProducto + "</td>" +
                "<td class='centro'>" + item.cantidad + "</td>" +
                "<td class='derecha'>$ " + item.precioUnitario + "</td>" +
                "<td class='derecha'>$ " + importe + "</td>" +
            "</tr>";
    }

    if (itemsPedido.length === 0) {
        filas = "<tr><td colspan='4' class='centro'>Todavía no hay productos en el pedido</td></tr>";
    }

    $("#cuerpoTabla").html(filas);
    $("#subtotalTexto").text("$ " + subtotal);
    $("#totalTexto").text("$ " + subtotal);   // si manejas IVA, súmalo aquí
}


/* ---------- POST: agregar un producto al pedido ---------- */
function agregarProducto() {

    var idProducto = $("#productoSeleccionado").val();
    var cantidad = Number($("#cantidadProducto").val());

    if (!idProducto || cantidad <= 0) {
        mostrarMensaje("Selecciona un producto y una cantidad válida", "error");
        return;
    }

    var detalleNuevo = {
        idMesa: Number(idMesaActual),
        idProducto: Number(idProducto),
        cantidad: cantidad
    };

    $.ajax({
        url: URL_DETALLE_PEDIDOS,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(detalleNuevo),

        success: function (response) {
            mostrarMensaje("Producto agregado al pedido", "exito");
            $("#cantidadProducto").val(1);
            cargarPedidoDeLaMesa();     // recargamos la tabla
        },

        error: function (xhr) {
            mostrarMensaje("No se pudo agregar el producto. Error " + xhr.status, "error");
        }
    });
}


/* ---------- POST: registrar el pago y cerrar la cuenta ---------- */
function registrarPago() {

    if (itemsPedido.length === 0) {
        mostrarMensaje("Esta mesa no tiene productos en el pedido", "error");
        return;
    }

    if (!confirm("¿Confirmar el pago y cerrar la cuenta de esta mesa?")) {
        return;
    }

    var total = Number($("#totalTexto").text().replace("$", "").trim());

    var comprobante = {
        idMesa: Number(idMesaActual),
        total: total
    };

    $.ajax({
        url: URL_COMPROBANTES,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(comprobante),

        success: function (response) {
            alert("Pago registrado correctamente");
            window.location.href = "mesas.html";
        },

        error: function (xhr) {
            mostrarMensaje("No se pudo registrar el pago. Error " + xhr.status, "error");
        }
    });
}


function mostrarMensaje(texto, tipo) {
    $("#mensaje").text(texto);
    $("#mensaje").attr("class", "mensaje " + tipo);
}
