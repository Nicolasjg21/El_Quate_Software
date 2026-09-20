/* ==========================================================================
   LAYOUT — menú lateral + encabezado + arranque común de cada pantalla
   --------------------------------------------------------------------------
   Cada HTML declara en <body>:  data-pagina  data-titulo  data-subtitulo
   y contiene <div class="menu" id="menu"> y <div id="cabecera">. Agregar una
   pantalla nueva = crear su HTML y sumar UNA línea en ITEMS.
   ========================================================================== */
(function (App) {
  "use strict";

  var ui = App.ui;
  var layout = {};

  var ITEMS = [
    { id: "inicio",      href: "inicio.html",      icono: "🏠", texto: "Panel Principal" },
    { id: "mesas",       href: "mesas.html",       icono: "🍽️", texto: "Mesas" },
    { id: "productos",   href: "productos.html",   icono: "📦", texto: "Inventario" },
    { id: "kardex",      href: "kardex.html",      icono: "📒", texto: "Kardex" },
    { id: "compras",     href: "compras.html",     icono: "🛒", texto: "Compras" },
    { id: "reportes",    href: "reportes.html",    icono: "📊", texto: "Ventas y Analíticas" },
    { id: "historiales", href: "historiales.html", icono: "🧾", texto: "Historiales" },
    { id: "catalogos",   href: "catalogos.html",   icono: "🗂️", texto: "Catálogos" },
    { id: "auditoria",   href: "auditoria.html",   icono: "🕘", texto: "Auditoría" },
    { id: "usuarios",    href: "usuarios.html",    icono: "👥", texto: "Usuarios" }
  ];

  layout.montar = function () {
    var cuerpo = document.body;
    var actual = cuerpo.dataset.pagina || "";
    var activa = actual === "cuenta" ? "mesas" : actual;          // la cuenta cuelga de Mesas
    var raiz = App.config.RAIZ;
    var sesion = App.sesion.datos() || {};
    var nombre = sesion.nombre || "Usuario";

    var menu = document.getElementById("menu");
    if (menu) {
      var enlaces = ITEMS.map(function (it) {
        return "<a href='" + it.href + "'" + (it.id === activa ? " class='activo'" : "") + ">" +
               "<span class='icono-menu'>" + it.icono + "</span>" + it.texto + "</a>";
      }).join("");
      menu.innerHTML =
        "<div class='marca-menu'>" +
          "<img src='" + raiz + "img/logo-menu.png' alt='El Cuate'>" +
          "<div><h2>El Cuate</h2><p class='submarca'>Sistema POS</p></div>" +
        "</div>" +
        "<div class='menu__enlaces'>" + enlaces + "</div>" +
        "<div class='menu__pie'>" +
          "<a href='configuracion.html'" + (actual === "configuracion" ? " class='activo'" : "") + "><span class='icono-menu'>⚙️</span>Configuración</a>" +
          "<a href='#' id='botonSalir'><span class='icono-menu'>🚪</span>Cerrar sesión</a>" +
        "</div>";
      document.getElementById("botonSalir").addEventListener("click", function (ev) {
        ev.preventDefault();
        App.sesion.cerrar();
      });
    }

    var cabecera = document.getElementById("cabecera");
    if (cabecera) {
      cabecera.innerHTML =
        "<div class='encabezado'>" +
          "<div><h1>" + ui.esc(cuerpo.dataset.titulo || "") + "</h1>" +
          "<p class='subtitulo'>" + ui.esc(cuerpo.dataset.subtitulo || "") + "</p></div>" +
          "<div class='encabezado__usuario'>" +
            "<div class='datos'><div class='nombre' id='nombreUsuarioTop'>" + ui.esc(nombre) + "</div>" +
            "<div class='fecha' id='fechaHoy'>" + ui.esc(new Date().toLocaleDateString("es-CO")) + "</div></div>" +
            "<div class='avatar' id='inicialUsuario'>" + ui.esc(nombre.charAt(0).toUpperCase()) + "</div>" +
          "</div>" +
        "</div>" +
        "<div id='mensaje' class='mensaje oculto' role='status' aria-live='polite'></div>";
      document.getElementById("mensaje").addEventListener("click", ui.ocultarAviso);
    }
  };

  /* Punto de entrada de TODAS las pantallas internas:
       App.pagina(async function () { ...cargar y pintar... });
     Protege la ruta, monta el layout, muestra un indicador de carga y, si algo
     falla, deja un bloque de error con botón "Reintentar" en vez de una pantalla
     en blanco. */
  App.pagina = function (iniciar) {
    document.addEventListener("DOMContentLoaded", function () {
      if (!App.sesion.proteger()) return;
      layout.montar();
      ui.mostrarFlash();

      var cabecera = document.getElementById("cabecera");
      var carga = document.createElement("div");
      carga.id = "cargando";
      carga.className = "cargando";
      carga.innerHTML = "<span class='spinner'></span><span id='textoCarga'>Cargando datos…</span>";
      if (cabecera) cabecera.insertAdjacentElement("afterend", carga);

      // Azure SQL (serverless) se pausa por inactividad: la primera consulta puede tardar.
      var pista = setTimeout(function () {
        ui.texto("textoCarga", "Sigue cargando… si la base de datos estaba en reposo, la primera consulta puede tardar hasta un minuto.");
      }, 6000);

      function terminar() { clearTimeout(pista); if (carga.parentNode) carga.parentNode.removeChild(carga); }

      Promise.resolve().then(iniciar).then(terminar, function (e) {
        terminar();
        console.error(e);
        var bloque = document.createElement("div");
        bloque.className = "estado-vacio estado-error";
        bloque.innerHTML = "<div class='icono-grande'>⚠️</div><h4>No se pudieron cargar los datos</h4>" +
          "<p>" + ui.esc(App.api.explicar(e)) + "</p>" +
          "<div class='acciones' style='justify-content:center'>" +
          "<button id='botonReintentar'>Reintentar</button>" +
          "<a class='boton-enlace' style='background:var(--gris)' href='configuracion.html'>Revisar conexión</a></div>";
        if (cabecera) cabecera.insertAdjacentElement("afterend", bloque);
        document.getElementById("botonReintentar").addEventListener("click", function () { window.location.reload(); });
      });
    });
  };

  App.layout = layout;
})(window.App = window.App || {});
