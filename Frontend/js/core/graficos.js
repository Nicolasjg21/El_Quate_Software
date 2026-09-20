/* ==========================================================================
   GRÁFICOS — SVG generado a mano (sin librerías)
   ========================================================================== */
(function (App) {
  "use strict";

  var esc = App.ui.esc;
  var PALETA = ["#F06543", "#F09D51", "#8B5CF6", "#0EA5E9", "#16A34A", "#DB2777", "#64748B"];
  var g = {};

  function corto(n) {
    n = Number(n) || 0;
    if (n >= 1e6) return (n / 1e6).toFixed(1).replace(/\.0$/, "") + "M";
    if (n >= 1e3) return Math.round(n / 1e3) + "k";
    return String(Math.round(n));
  }
  function vacio() { return "<p class='texto-suave' style='padding:20px 0;text-align:center'>Sin datos para mostrar en este período.</p>"; }
  function hayDatos(valores) { return valores.some(function (v) { return Number(v) > 0; }); }

  /* series: [{ nombre, valores:[…], color? }]  —  etiquetas: eje X */
  g.linea = function (series, etiquetas) {
    var todos = [];
    series.forEach(function (s) { todos = todos.concat(s.valores); });
    if (!etiquetas.length || !hayDatos(todos)) return vacio();

    var ancho = 560, alto = 180, izq = 44, der = 10, arriba = 10;
    var maximo = Math.max.apply(null, todos.concat([1]));
    var paso = etiquetas.length > 1 ? (ancho - izq - der) / (etiquetas.length - 1) : 0;
    var yDe = function (v) { return arriba + (alto - arriba) - (v / maximo) * (alto - arriba); };
    var xDe = function (i) { return etiquetas.length > 1 ? izq + paso * i : izq + (ancho - izq - der) / 2; };

    var rejilla = "";
    for (var k = 0; k <= 4; k++) {
      var v = (maximo / 4) * k, y = yDe(v);
      rejilla += "<line x1='" + izq + "' x2='" + (ancho - der) + "' y1='" + y + "' y2='" + y + "' stroke='#E5E7EB' stroke-width='1'/>" +
                 "<text x='" + (izq - 6) + "' y='" + (y + 3) + "' font-size='10' fill='#5C6062' text-anchor='end'>" + corto(v) + "</text>";
    }

    var cuerpo = series.map(function (s, si) {
      var color = s.color || PALETA[si % PALETA.length];
      var pts = s.valores.map(function (val, i) { return xDe(i) + "," + yDe(val); }).join(" ");
      var puntos = s.valores.map(function (val, i) {
        return "<circle cx='" + xDe(i) + "' cy='" + yDe(val) + "' r='3.5' fill='" + color + "'><title>" + esc(s.nombre + ": " + App.ui.moneda(val)) + "</title></circle>";
      }).join("");
      return "<polyline points='" + pts + "' fill='none' stroke='" + color + "' stroke-width='2.5'/>" + puntos;
    }).join("");

    var salto = Math.ceil(etiquetas.length / 12);
    var ejeX = etiquetas.map(function (e, i) {
      if (i % salto) return "";
      return "<text x='" + xDe(i) + "' y='" + (alto + 16) + "' font-size='10' fill='#5C6062' text-anchor='middle'>" + esc(e) + "</text>";
    }).join("");

    var leyenda = series.length > 1
      ? "<div class='leyenda'>" + series.map(function (s, si) { return "<span><i style='background:" + (s.color || PALETA[si % PALETA.length]) + "'></i>" + esc(s.nombre) + "</span>"; }).join("") + "</div>"
      : "";

    return "<svg viewBox='0 0 " + ancho + " " + (alto + 22) + "' style='width:100%;height:auto' role='img'>" + rejilla + cuerpo + ejeX + "</svg>" + leyenda;
  };

  /* valores + etiquetas; muestra el valor sobre cada barra. */
  g.barras = function (valores, etiquetas) {
    if (!valores.length || !hayDatos(valores)) return vacio();
    var ancho = 560, alto = 180, izq = 10, arriba = 18;
    var maximo = Math.max.apply(null, valores.concat([1]));
    var paso = (ancho - izq) / valores.length;
    var anchoBarra = Math.min(paso * 0.55, 90);

    var barras = valores.map(function (v, i) {
      var x = izq + paso * i + (paso - anchoBarra) / 2;
      var h = (v / maximo) * (alto - arriba);
      return "<rect x='" + x + "' y='" + (alto - h) + "' width='" + anchoBarra + "' height='" + Math.max(h, 1) + "' rx='3' fill='" + PALETA[1] + "'><title>" + esc(etiquetas[i] + ": " + App.ui.moneda(v)) + "</title></rect>" +
             "<text x='" + (x + anchoBarra / 2) + "' y='" + (alto - h - 4) + "' font-size='10' fill='#313638' text-anchor='middle'>" + corto(v) + "</text>" +
             "<text x='" + (x + anchoBarra / 2) + "' y='" + (alto + 16) + "' font-size='10' fill='#5C6062' text-anchor='middle'>" + esc(etiquetas[i]) + "</text>";
    }).join("");

    return "<svg viewBox='0 0 " + ancho + " " + (alto + 22) + "' style='width:100%;height:auto' role='img'>" + barras + "</svg>";
  };

  /* datos: [{ nombre, valor }] — dona + leyenda con porcentajes. */
  g.dona = function (datos) {
    var total = datos.reduce(function (s, d) { return s + (Number(d.valor) || 0); }, 0);
    if (!datos.length || total <= 0) return vacio();

    var tam = 180, c = tam / 2, radio = 62, grosor = 26, angulo = -Math.PI / 2;
    var sectores = datos.map(function (d, i) {
      var porcion = (Number(d.valor) || 0) / total;
      if (porcion <= 0) return "";
      var color = PALETA[i % PALETA.length];
      var titulo = "<title>" + esc(d.nombre + ": " + App.ui.moneda(d.valor) + " (" + Math.round(porcion * 100) + "%)") + "</title>";
      if (porcion >= 0.9999) {     // un único sector: un arco de 360° no se dibuja, se usa un círculo
        return "<circle cx='" + c + "' cy='" + c + "' r='" + radio + "' fill='none' stroke='" + color + "' stroke-width='" + grosor + "'>" + titulo + "</circle>";
      }
      var fin = angulo + porcion * Math.PI * 2;
      var x1 = c + radio * Math.cos(angulo), y1 = c + radio * Math.sin(angulo);
      var x2 = c + radio * Math.cos(fin), y2 = c + radio * Math.sin(fin);
      var grande = porcion > 0.5 ? 1 : 0;
      angulo = fin;
      return "<path d='M " + x1 + " " + y1 + " A " + radio + " " + radio + " 0 " + grande + " 1 " + x2 + " " + y2 + "' fill='none' stroke='" + color + "' stroke-width='" + grosor + "'>" + titulo + "</path>";
    }).join("");

    var leyenda = "<div class='leyenda' style='justify-content:center'>" + datos.map(function (d, i) {
      return "<span><i style='background:" + PALETA[i % PALETA.length] + "'></i>" + esc(d.nombre) + " · " + Math.round(((Number(d.valor) || 0) / total) * 100) + "%</span>";
    }).join("") + "</div>";

    return "<svg viewBox='0 0 " + tam + " " + tam + "' style='width:180px;margin:0 auto;display:block' role='img'>" + sectores + "</svg>" + leyenda;
  };

  App.graficos = g;
})(window.App = window.App || {});
