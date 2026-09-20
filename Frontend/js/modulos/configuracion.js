/* ==========================================================================
   CONFIGURACIÓN — sesión, URL del backend y diagnóstico de endpoints.
   El diagnóstico recorre TODOS los GET y muestra qué responde cada uno;
   además lista los valores REALES de los campos de estado para que se
   copien a js/core/config.js (ESTADOS).
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;

  App.pagina(async function () {
    var s = App.sesion.datos() || {};
    ui.html("datosSesion",
      "<span>Usuario <strong>" + ui.esc(s.nombre || "—") + "</strong></span>" +
      "<span>Correo <strong>" + ui.esc(s.email || "—") + "</strong></span>" +
      "<span>idUsuario <strong>" + ui.esc(s.idUsuario == null ? "no identificado" : s.idUsuario) + "</strong></span>" +
      "<span>idRol <strong>" + ui.esc(s.idRol == null ? "—" : s.idRol) + "</strong></span>" +
      "<span>Token expira <strong>" + ui.esc(s.exp ? ui.fecha(new Date(s.exp * 1000)) : "sin fecha") + "</strong></span>");

    ui.poner("apiUrl", cfg.API_URL);
    document.getElementById("botonGuardarUrl").addEventListener("click", function () {
      cfg.cambiarApiUrl(ui.valor("apiUrl"));
      ui.aviso("URL guardada. Recargando…", "exito");
      setTimeout(function () { window.location.reload(); }, 600);
    });
    document.getElementById("botonRestablecer").addEventListener("click", function () {
      cfg.cambiarApiUrl("");
      ui.aviso("URL restablecida a " + cfg.API_POR_DEFECTO + ". Recargando…", "exito");
      setTimeout(function () { window.location.reload(); }, 600);
    });
    document.getElementById("botonProbar").addEventListener("click", diagnosticar);
  });

  async function diagnosticar() {
    var claves = Object.keys(cfg.ENTIDADES);
    ui.html("cuerpoDiagnostico", "<tr><td colspan='5' class='centro texto-suave'>Probando " + claves.length + " endpoints…</td></tr>");

    await ui.conBoton(document.getElementById("botonProbar"), async function () {
      var resultados = [];
      for (var i = 0; i < claves.length; i++) {
        var clave = claves[i];
        var r = await App.api.probar(clave);
        r.clave = clave;
        resultados.push(r);
      }

      ui.html("cuerpoDiagnostico", resultados.map(function (r) {
        var e = cfg.ENTIDADES[r.clave];
        return "<tr><td>" + ui.esc(e.ctl) + "</td>" +
          "<td><code class='inline'>GET /api/" + ui.esc(e.ctl) + "/" + ui.esc(e.listar) + "</code></td>" +
          "<td class='" + (r.ok ? "diag-ok" : "diag-mal") + "'>" + (r.ok ? "OK" : ui.esc(r.error)) + "</td>" +
          "<td class='derecha'>" + (r.ok ? r.filas.length : "—") + "</td>" +
          "<td class='derecha texto-suave'>" + r.ms + "</td></tr>";
      }).join(""));

      var fallos = resultados.filter(function (r) { return !r.ok; }).length;
      ui.aviso(fallos ? fallos + " endpoint(s) respondieron con error. Revisa la columna Resultado."
                      : "Los " + resultados.length + " endpoints de lectura respondieron correctamente.",
               fallos ? "error" : "exito");

      mostrarValoresEstado(resultados);
    });
  }

  /* Muestra los textos reales de los campos de estado, que son cadenas libres
     y deben coincidir exactamente con los de config.js → ESTADOS. */
  function mostrarValoresEstado(resultados) {
    function datosDe(clave) { var r = resultados.filter(function (x) { return x.clave === clave; })[0]; return (r && r.ok) ? r.filas : []; }
    function distintos(filas, campo) {
      var vistos = [];
      filas.forEach(function (f) { if (f[campo] != null && vistos.indexOf(f[campo]) === -1) vistos.push(f[campo]); });
      return vistos;
    }

    var grupos = [
      { titulo: "mesas.estado", valores: distintos(datosDe("mesas"), "estado"), esperado: [cfg.ESTADOS.MESA.LIBRE, cfg.ESTADOS.MESA.OCUPADA] },
      { titulo: "cuentas.estado", valores: distintos(datosDe("cuentas"), "estado"), esperado: [cfg.ESTADOS.CUENTA.ABIERTA, cfg.ESTADOS.CUENTA.CERRADA] },
      { titulo: "pedidos.estadoPedido", valores: distintos(datosDe("pedidos"), "estadoPedido"), esperado: [cfg.ESTADOS.PEDIDO.PENDIENTE] },
      { titulo: "kardex.tipoMovimiento", valores: distintos(datosDe("kardex"), "tipoMovimiento"), esperado: [cfg.ESTADOS.KARDEX.ENTRADA, cfg.ESTADOS.KARDEX.SALIDA] }
    ];

    ui.html("valoresEstado",
      "<h3 style='margin-bottom:8px'>Valores de estado encontrados en la base de datos</h3>" +
      "<p class='texto-suave texto-xs' style='margin-bottom:10px'>Si alguno no coincide con lo que espera el frontend, copia el texto exacto a <code class='inline'>js/core/config.js → ESTADOS</code>.</p>" +
      grupos.map(function (g) {
        var desconocidos = g.valores.filter(function (v) {
          return !g.esperado.some(function (e) { return cfg.igual(e, v); });
        });
        return "<div style='margin-bottom:10px'><strong>" + ui.esc(g.titulo) + "</strong>: " +
          (g.valores.length ? g.valores.map(function (v) {
            var ok = g.esperado.some(function (e) { return cfg.igual(e, v); });
            return ui.badge(v, ok ? "verde" : "rojo");
          }).join(" ") : "<span class='texto-suave'>sin datos todavía</span>") +
          (desconocidos.length ? "<div class='texto-xs' style='color:#DC2626;margin-top:4px'>El frontend espera: " +
            ui.esc(g.esperado.join(" / ")) + "</div>" : "") + "</div>";
      }).join(""));
  }
})(window.App = window.App || {});
