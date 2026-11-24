/**
 * @file ayuda-contextual.js
 * @description Botón "?" del encabezado (RF15). Abre la página de Ayuda en la sección de la pantalla actual
 *   e indica desde qué pantalla se abrió, para que "Reportar problema" incluya ese dato.
 *   Se carga en todas las páginas con encabezado, después de config.js.
 */

(function () {
  "use strict";

  /** Sección de la ayuda que corresponde a cada página. */
  const SECCION_POR_PAGINA = {
    "dashboard.html": "reportes",
    "analiticas.html": "reportes",
    "auditoria.html": "reportes",
    "mesas.html": "mesas",
    "inventario.html": "inventario",
    "proveedores.html": "inventario",
    "historiales.html": "cobros",
    "usuarios.html": "usuarios",
    "configuracion.html": "cuenta"
  };

  document.addEventListener("DOMContentLoaded", () => {
    const pagina = (location.pathname.split("/").pop() || "").toLowerCase();
    const destino = document.querySelector(".header-usuario");
    if (!destino || pagina === "ayuda.html") return;

    const seccion = SECCION_POR_PAGINA[pagina] || "";
    const enlace = document.createElement("a");
    enlace.className = "btn-ayuda-contextual";
    enlace.href = "ayuda.html?desde=" + encodeURIComponent(pagina) + (seccion ? "#" + seccion : "");
    enlace.title = "Ayuda de esta pantalla";
    enlace.setAttribute("aria-label", "Ayuda de esta pantalla");
    enlace.textContent = "?";
    destino.insertBefore(enlace, destino.firstChild);
  });
})();
