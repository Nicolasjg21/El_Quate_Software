
function graficoLinea(valores, etiquetas) {
    var ancho = 560, alto = 180, margenIzq = 34;
    var maximo = Math.max.apply(null, valores.concat([1]));
    var paso = (ancho - margenIzq - 10) / (etiquetas.length - 1 || 1);

    var puntos = valores.map(function (v, i) {
        return (margenIzq + paso * i) + "," + (alto - (v / maximo) * alto);
    }).join(" ");

    var circulos = valores.map(function (v, i) {
        var x = margenIzq + paso * i, y = alto - (v / maximo) * alto;
        return '<circle cx="' + x + '" cy="' + y + '" r="3.5" fill="#F06543"></circle>';
    }).join("");

    var ejeX = etiquetas.map(function (e, i) {
        return '<text x="' + (margenIzq + paso * i) + '" y="' + (alto + 16) +
               '" font-size="10" fill="#5C6062" text-anchor="middle">' + e + '</text>';
    }).join("");

    return '<svg viewBox="0 0 ' + ancho + ' ' + (alto + 20) + '" style="width:100%;height:auto">' +
        '<polyline points="' + puntos + '" fill="none" stroke="#F06543" stroke-width="2.5"/>' +
        circulos + ejeX + '</svg>';
}


function graficoBarras(valores, etiquetas) {
    var ancho = 560, alto = 180, margenIzq = 10;
    var maximo = Math.max.apply(null, valores.concat([1]));
    var anchoBarra = (ancho - margenIzq) / valores.length * 0.55;
    var paso = (ancho - margenIzq) / valores.length;

    var barras = valores.map(function (v, i) {
        var x = margenIzq + paso * i + (paso - anchoBarra) / 2;
        var h = (v / maximo) * alto;
        return '<rect x="' + x + '" y="' + (alto - h) + '" width="' + anchoBarra +
               '" height="' + Math.max(h, 1) + '" rx="3" fill="#F09D51"></rect>' +
               '<text x="' + (x + anchoBarra / 2) + '" y="' + (alto + 16) +
               '" font-size="10" fill="#5C6062" text-anchor="middle">' + etiquetas[i] + '</text>';
    }).join("");

    return '<svg viewBox="0 0 ' + ancho + ' ' + (alto + 20) + '" style="width:100%;height:auto">' + barras + '</svg>';
}


function graficoDona(datos) {
    var tam = 180, centro = tam / 2, radio = 62, grosor = 26;
    var colores = ["#F06543", "#F09D51", "#8B5CF6", "#0EA5E9", "#16A34A"];
    var total = datos.reduce(function (s, d) { return s + d.valor; }, 0) || 1;
    var angulo = -Math.PI / 2;

    var sectores = datos.map(function (d, i) {
        var porcion = d.valor / total;
        var fin = angulo + porcion * Math.PI * 2;
        var x1 = centro + radio * Math.cos(angulo), y1 = centro + radio * Math.sin(angulo);
        var x2 = centro + radio * Math.cos(fin), y2 = centro + radio * Math.sin(fin);
        var grande = porcion > 0.5 ? 1 : 0;
        angulo = fin;
        return '<path d="M ' + x1 + ' ' + y1 + ' A ' + radio + ' ' + radio + ' 0 ' + grande + ' 1 ' + x2 + ' ' + y2 +
               '" fill="none" stroke="' + colores[i % colores.length] + '" stroke-width="' + grosor + '"></path>';
    }).join("");

    return '<svg viewBox="0 0 ' + tam + ' ' + tam + '" style="width:180px;margin:0 auto;display:block">' + sectores + '</svg>';
}
