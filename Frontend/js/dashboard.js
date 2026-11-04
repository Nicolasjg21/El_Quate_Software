/**
 * @file dashboard.js
 * @description Panel Principal (requiere "auditorias.consultar"). Resumen de los últimos 7 días
 *   frente a los 7 anteriores, tendencia de ventas y órdenes, ingresos por categoría y productos
 *   más vendidos en el período elegido, y estado del inventario con búsqueda.
 *   Las gráficas se dibujan como SVG generado con JavaScript: se adaptan al ancho del panel y se
 *   vuelven a dibujar al cambiar el tamaño de la ventana.
 * @requires api-negocio.js, data.js, auth.js
 */

(function () {
  const $ = (id) => document.getElementById(id);
  let inventario = [];
  let ordenes = [];          // órdenes pagadas con sus líneas (producto, categoría, cantidad, precio)
  let desdeTendencia = null; // primer día de la gráfica de tendencia

  /* ---------- Utilidades ---------- */
  const DIAS = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
  const NS = "http://www.w3.org/2000/svg";
  const inicioDia = (f) => { const d = new Date(f); d.setHours(0, 0, 0, 0); return d; };
  const claveDia = (f) => `${f.getFullYear()}-${f.getMonth()}-${f.getDate()}`;
  const fechaDe = (o) => new Date(o.fechaHora);

  document.addEventListener("DOMContentLoaded", () => {
    const f = document.querySelector(".header-usuario-fecha");
    if (f) f.textContent = fechaHoy();
    $("buscar-panel-inventario").addEventListener("input", renderInventario);
    $("periodo-categorias").addEventListener("change", dibujarPeriodo);

    /* Las gráficas toman el ancho real del panel: al cambiar el tamaño se vuelven a dibujar. */
    let espera = null;
    window.addEventListener("resize", () => {
      clearTimeout(espera);
      espera = setTimeout(() => { if (desdeTendencia) { dibujarTendencia(); dibujarPeriodo(); } }, 150);
    });

    Auth.listo.then(cargar);
  });

  /** Muestra la variación porcentual frente a los 7 días previos. */
  function tendencia(el, actual, previo) {
    if (!previo) {
      el.className = "kpi-tendencia";
      el.textContent = actual ? "Sin datos en los 7 días previos" : "Sin movimiento en 14 días";
      return;
    }
    const pct = Math.round(((actual - previo) / previo) * 100);
    el.className = "kpi-tendencia " + (pct >= 0 ? "kpi-tendencia--positiva" : "kpi-tendencia--negativa");
    el.textContent = `${pct >= 0 ? "↑" : "↓"} ${Math.abs(pct)}% vs 7 días previos`;
  }

  /** Carga ventas, órdenes con sus líneas e inventario y dibuja el panel. */
  async function cargar() {
    const estado = $("estado-panel");
    estado.hidden = false;
    estado.textContent = "Cargando información...";

    const resultados = await Promise.allSettled([
      Negocio.ventas(), Negocio.ordenesConItems(), Negocio.productos()
    ]);
    const [rVentas, rOrdenes, rProductos] = resultados;
    const fallos = resultados.filter((r) => r.status === "rejected");
    if (fallos.length === resultados.length) {
      estado.textContent = Api.mensajeError(fallos[0].reason, "No fue posible cargar el panel. Intente de nuevo.");
      return;
    }
    estado.hidden = fallos.length === 0;
    if (fallos.length) estado.textContent = "Algunos datos no pudieron cargarse: " + Api.mensajeError(fallos[0].reason, "error de conexión");

    ordenes = rOrdenes.status === "fulfilled" ? rOrdenes.value.ordenes : [];
    const ventas = rVentas.status === "fulfilled" ? rVentas.value : null;

    const totalIngresos = ventas ? ventas.totalIngresos : ordenes.reduce((a, o) => a + o.total, 0);
    const totalOrdenes = ventas ? ventas.totalVentas : ordenes.length;
    const promedio = ventas ? ventas.promedioVenta : (ordenes.length ? totalIngresos / ordenes.length : 0);
    $("kpi-ventas").textContent = formatoCOP(totalIngresos);
    $("kpi-ordenes").textContent = totalOrdenes.toLocaleString("es-CO");
    $("kpi-promedio").textContent = formatoCOP(promedio);

    const hoy = inicioDia(new Date());
    const hace7 = new Date(hoy); hace7.setDate(hace7.getDate() - 6);
    const hace14 = new Date(hoy); hace14.setDate(hace14.getDate() - 13);
    const fin = new Date(hoy); fin.setDate(fin.getDate() + 1);
    const enRango = (o, d, h) => { const t = fechaDe(o); return t >= d && t < h; };
    const sem = ordenes.filter((o) => enRango(o, hace7, fin));
    const prev = ordenes.filter((o) => enRango(o, hace14, hace7));
    const sumaSem = sem.reduce((a, o) => a + o.total, 0);
    const sumaPrev = prev.reduce((a, o) => a + o.total, 0);
    tendencia($("kpi-ventas-t"), sumaSem, sumaPrev);
    tendencia($("kpi-ordenes-t"), sem.length, prev.length);
    tendencia($("kpi-promedio-t"), sem.length ? sumaSem / sem.length : 0, prev.length ? sumaPrev / prev.length : 0);

    if (rProductos.status === "fulfilled") {
      inventario = rProductos.value;
      const bajos = inventario.filter((p) => p.stock <= p.stockMinimo).length;
      $("kpi-articulos").textContent = inventario.length.toLocaleString("es-CO");
      const t = $("kpi-articulos-t");
      t.className = "kpi-tendencia " + (bajos ? "kpi-tendencia--negativa" : "kpi-tendencia--positiva");
      t.textContent = bajos ? `${bajos} con stock bajo o agotado` : "Todo en existencia";
      renderInventario();
    } else {
      $("tabla-estado-inv").innerHTML = `<tr><td colspan="7" class="col-centro">No fue posible cargar el inventario.</td></tr>`;
    }

    desdeTendencia = hace7;
    dibujarTendencia();

    if (rOrdenes.status === "fulfilled") {
      dibujarPeriodo();
    } else {
      $("tabla-top").innerHTML = `<tr><td colspan="4" class="col-centro">No fue posible cargar los productos más vendidos.</td></tr>`;
      $("chart-categorias").innerHTML = `<p class="grafica-vacia">No fue posible cargar las ventas.</p>`;
    }
  }

  /* ==========================================================================
     SVG: utilidades de dibujo
     ========================================================================== */

  /** Crea un elemento SVG con sus atributos (y texto opcional). */
  function el(tag, attrs, texto) {
    const n = document.createElementNS(NS, tag);
    Object.keys(attrs || {}).forEach((k) => n.setAttribute(k, attrs[k]));
    if (texto !== undefined) n.textContent = texto;
    return n;
  }

  /** Lienzo SVG del ancho real del contenedor. */
  function lienzo(cont, alto) {
    const ancho = Math.max(280, Math.floor(cont.clientWidth || 600));
    const svg = el("svg", { width: ancho, height: alto, viewBox: `0 0 ${ancho} ${alto}` });
    return { svg, ancho, alto };
  }

  /** Valor máximo "redondo" para el eje (1, 2, 2.5, 5 o 10 × potencia de 10), dividido en 4 tramos. */
  function techo(max, minimo) {
    const m = Math.max(max, minimo || 0);
    if (m <= 0) return 4;
    const tramo = m / 4;
    const mag = Math.pow(10, Math.floor(Math.log10(tramo)));
    const n = tramo / mag;
    const paso = (n <= 1 ? 1 : n <= 2 ? 2 : n <= 2.5 ? 2.5 : n <= 5 ? 5 : 10) * mag;
    return paso * 4;
  }

  /** Abrevia valores del eje: 1.500 → 1,5 k ; 2.000.000 → 2 M. */
  function abreviar(n) {
    if (n >= 1e6) return (n / 1e6).toLocaleString("es-CO", { maximumFractionDigits: 1 }) + " M";
    if (n >= 1e3) return (n / 1e3).toLocaleString("es-CO", { maximumFractionDigits: 1 }) + " k";
    return n.toLocaleString("es-CO", { maximumFractionDigits: 1 });
  }

  /**
   * Curva suave que pasa por todos los puntos sin inventar subidas ni bajadas entre ellos
   * (interpolación cúbica monótona de Fritsch-Carlson).
   */
  function curva(pts) {
    const n = pts.length;
    if (!n) return "";
    if (n === 1) return `M${pts[0].x},${pts[0].y}`;
    const s = [], t = [];
    for (let i = 0; i < n - 1; i++) s.push((pts[i + 1].y - pts[i].y) / (pts[i + 1].x - pts[i].x));
    t[0] = s[0]; t[n - 1] = s[n - 2];
    for (let i = 1; i < n - 1; i++) t[i] = s[i - 1] * s[i] <= 0 ? 0 : (s[i - 1] + s[i]) / 2;
    for (let i = 0; i < n - 1; i++) {
      if (s[i] === 0) { t[i] = 0; t[i + 1] = 0; continue; }
      const a = t[i] / s[i], b = t[i + 1] / s[i], h = a * a + b * b;
      if (h > 9) { const k = 3 / Math.sqrt(h); t[i] = k * a * s[i]; t[i + 1] = k * b * s[i]; }
    }
    let d = `M${pts[0].x},${pts[0].y}`;
    for (let i = 0; i < n - 1; i++) {
      const p = pts[i], q = pts[i + 1], dx = (q.x - p.x) / 3;
      d += ` C${(p.x + dx).toFixed(1)},${(p.y + t[i] * dx).toFixed(1)} ${(q.x - dx).toFixed(1)},${(q.y - t[i + 1] * dx).toFixed(1)} ${q.x},${q.y}`;
    }
    return d;
  }

  /* ==========================================================================
     Tendencia de ventas y órdenes (últimos 7 días)
     ========================================================================== */
  function dibujarTendencia() {
    const cont = $("chart-tendencia");
    const dias = [];
    for (let i = 0; i < 7; i++) { const d = new Date(desdeTendencia); d.setDate(d.getDate() + i); dias.push(d); }
    const ventas = new Map(dias.map((d) => [claveDia(d), 0]));
    const cuenta = new Map(dias.map((d) => [claveDia(d), 0]));
    ordenes.forEach((o) => {
      const k = claveDia(fechaDe(o));
      if (ventas.has(k)) { ventas.set(k, ventas.get(k) + o.total); cuenta.set(k, cuenta.get(k) + 1); }
    });
    const sv = dias.map((d) => ventas.get(claveDia(d)));
    const so = dias.map((d) => cuenta.get(claveDia(d)));

    const { svg, ancho, alto } = lienzo(cont, 260);
    const m = { arriba: 14, derecha: 44, abajo: 30, izquierda: 60 };
    const aw = ancho - m.izquierda - m.derecha, ah = alto - m.arriba - m.abajo;
    const base = m.arriba + ah;
    const maxV = techo(Math.max(...sv));
    const maxO = techo(Math.max(...so), 4);
    const x = (i) => m.izquierda + (aw * i) / (dias.length - 1);
    const yV = (v) => base - (v / maxV) * ah;
    const yO = (v) => base - (v / maxO) * ah;

    /* Cuadrícula y ejes: valores de ventas a la izquierda, órdenes a la derecha, días abajo. */
    for (let i = 0; i <= 4; i++) {
      const y = base - (ah * i) / 4;
      svg.appendChild(el("line", { class: i === 0 ? "g-base" : "g-guia", x1: m.izquierda, x2: m.izquierda + aw, y1: y, y2: y }));
      svg.appendChild(el("text", { class: "g-eje", x: m.izquierda - 8, y: y + 4, "text-anchor": "end" }, i === 0 ? "0" : abreviar((maxV * i) / 4)));
      svg.appendChild(el("text", { class: "g-eje", x: m.izquierda + aw + 8, y: y + 4, "text-anchor": "start" }, abreviar((maxO * i) / 4)));
    }
    dias.forEach((d, i) => {
      if (i > 0 && i < dias.length - 1) svg.appendChild(el("line", { class: "g-guia", x1: x(i), x2: x(i), y1: m.arriba, y2: base }));
      svg.appendChild(el("text", { class: "g-eje g-eje--x", x: x(i), y: alto - 8, "text-anchor": "middle" }, DIAS[d.getDay()]));
    });

    /* Series: primero órdenes (debajo) y luego ventas (encima). */
    const serie = (valores, y, clase, texto) => {
      const pts = valores.map((v, i) => ({ x: x(i), y: +y(v).toFixed(1), v }));
      svg.appendChild(el("path", { class: `g-linea g-linea--${clase}`, d: curva(pts) }));
      pts.forEach((p, i) => {
        const c = el("circle", { class: `g-punto g-punto--${clase}`, cx: p.x, cy: p.y, r: 4.5 });
        c.appendChild(el("title", {}, `${DIAS[dias[i].getDay()]} ${dias[i].getDate()}: ${texto(p.v)}`));
        svg.appendChild(c);
      });
    };
    serie(so, yO, "ordenes", (v) => `${v} ${v === 1 ? "orden" : "órdenes"}`);
    serie(sv, yV, "ventas", (v) => formatoCOP(v));

    if (!sv.some((v) => v > 0)) {
      svg.appendChild(el("text", { class: "g-eje g-eje--x", x: m.izquierda + aw / 2, y: m.arriba + ah / 2, "text-anchor": "middle" }, "Sin ventas en los últimos 7 días"));
    }
    cont.replaceChildren(svg);
  }

  /* ==========================================================================
     Ingresos por categoría y productos más vendidos (período elegido)
     ========================================================================== */

  /** Rango [desde, hasta) del período seleccionado: últimos 7 días, últimos 30 días o este año. */
  function rangoPeriodo(valor) {
    const hoy = inicioDia(new Date());
    const hasta = new Date(hoy); hasta.setDate(hasta.getDate() + 1);
    let desde;
    if (valor === "anio") desde = new Date(hoy.getFullYear(), 0, 1);
    else { desde = new Date(hoy); desde.setDate(desde.getDate() - (Number(valor) - 1)); }
    return { desde, hasta };
  }

  function dibujarPeriodo() {
    const selector = $("periodo-categorias");
    const etiqueta = selector.options[selector.selectedIndex].text;
    const { desde, hasta } = rangoPeriodo(selector.value);

    const porProducto = new Map();
    ordenes.forEach((o) => {
      const t = fechaDe(o);
      if (t < desde || t >= hasta) return;
      (o.items || []).forEach((it) => {
        const f = porProducto.get(it.idProducto) || { nombre: it.nombre, categoria: it.categoria, unidades: 0, ingresos: 0 };
        f.unidades += it.cantidad;
        f.ingresos += it.cantidad * it.precioUnitario;
        porProducto.set(it.idProducto, f);
      });
    });
    const productos = [...porProducto.values()].sort((a, b) => b.unidades - a.unidades || b.ingresos - a.ingresos);
    const porCategoria = new Map();
    productos.forEach((p) => porCategoria.set(p.categoria, (porCategoria.get(p.categoria) || 0) + p.ingresos));
    const categorias = [...porCategoria.entries()].map(([nombre, ingresos]) => ({ nombre, ingresos }))
      .sort((a, b) => b.ingresos - a.ingresos);

    $("periodo-top").textContent = etiqueta;
    $("chart-categorias").setAttribute("aria-label", `Ingresos por categoría: ${etiqueta.toLowerCase()}`);
    dibujarCategorias(categorias, etiqueta);
    dibujarTop(productos.slice(0, 5), etiqueta);
  }

  /**
   * Barras verticales: valores a la izquierda, categorías abajo. Muestra las 4 categorías con más
   * ingresos; si hay más, el resto se suma en "Otras" para que los nombres quepan debajo de cada barra.
   */
  function dibujarCategorias(cats, etiqueta) {
    const cont = $("chart-categorias");
    const top = cats.length > 5
      ? cats.slice(0, 4).concat({ nombre: "Otras", ingresos: cats.slice(4).reduce((a, c) => a + c.ingresos, 0) })
      : cats;
    if (!top.length) {
      cont.innerHTML = `<p class="grafica-vacia">Sin ventas en ${esc(etiqueta.toLowerCase())}.</p>`;
      return;
    }

    const { svg, ancho, alto } = lienzo(cont, 260);
    const m = { arriba: 14, derecha: 12, abajo: 30, izquierda: 56 };
    const aw = ancho - m.izquierda - m.derecha, ah = alto - m.arriba - m.abajo;
    const base = m.arriba + ah;
    const max = techo(Math.max(...top.map((c) => c.ingresos)));
    const banda = aw / top.length;
    const barra = Math.min(banda * 0.62, 72);
    const radio = 4;

    for (let i = 0; i <= 4; i++) {
      const y = base - (ah * i) / 4;
      svg.appendChild(el("line", { class: i === 0 ? "g-base" : "g-guia", x1: m.izquierda, x2: m.izquierda + aw, y1: y, y2: y }));
      svg.appendChild(el("text", { class: "g-eje", x: m.izquierda - 8, y: y + 4, "text-anchor": "end" }, i === 0 ? "0" : abreviar((max * i) / 4)));
    }

    const tamLetra = banda < 70 ? 10 : 11;
    const maxLetras = Math.max(4, Math.floor(banda / (tamLetra * 0.56)));
    top.forEach((c, i) => {
      const cx = m.izquierda + banda * i + banda / 2;
      const h = Math.max(2, (c.ingresos / max) * ah);
      const x0 = cx - barra / 2, y0 = base - h;
      const r = Math.min(radio, h, barra / 2);
      /* Barra con las esquinas de arriba redondeadas. */
      const d = `M${x0},${base} V${y0 + r} Q${x0},${y0} ${x0 + r},${y0} H${x0 + barra - r} Q${x0 + barra},${y0} ${x0 + barra},${y0 + r} V${base} Z`;
      const b = el("path", { class: "g-barra", d });
      b.appendChild(el("title", {}, `${c.nombre}: ${formatoCOP(c.ingresos)}`));
      svg.appendChild(b);

      const nombre = c.nombre.length > maxLetras ? c.nombre.slice(0, maxLetras - 1) + "…" : c.nombre;
      const t = el("text", { class: "g-eje g-eje--x", x: cx, y: alto - 8, "text-anchor": "middle", style: `font-size:${tamLetra}px` }, nombre);
      t.appendChild(el("title", {}, c.nombre));
      svg.appendChild(t);
    });
    cont.replaceChildren(svg);
  }

  function dibujarTop(filas, etiqueta) {
    $("tabla-top").innerHTML = filas.length ? filas.map((f) => `
      <tr>
        <td>${esc(f.nombre)}</td>
        <td>${esc(f.categoria)}</td>
        <td class="col-derecha">${f.unidades.toLocaleString("es-CO")}</td>
        <td class="col-derecha">${formatoCOP(f.ingresos)}</td>
      </tr>`).join("") : `<tr><td colspan="4" class="col-centro">Sin ventas en ${esc(etiqueta.toLowerCase())}.</td></tr>`;
  }

  /** Estado del inventario: En existencia, Stock bajo (≤ mínimo) o Sin existencias. */
  function renderInventario() {
    const q = $("buscar-panel-inventario").value.trim().toLowerCase();
    const filas = inventario.filter((p) => {
      const cod = "prd-" + String(p.idProducto).padStart(4, "0");
      return !q || p.nombre.toLowerCase().includes(q) || p.categoria.toLowerCase().includes(q) || cod.includes(q);
    }).sort((a, b) => a.nombre.localeCompare(b.nombre));

    $("tabla-estado-inv").innerHTML = filas.length ? filas.map((p) => {
      let pill = `<span class="pill pill--stock">En Existencia</span>`;
      if (p.stock <= 0) pill = `<span class="pill pill--negativa">Sin Existencias</span>`;
      else if (p.stock <= p.stockMinimo) pill = `<span class="pill pill--negativa">Stock Bajo</span>`;
      return `<tr>
        <td class="col-mono">PRD-${String(p.idProducto).padStart(4, "0")}</td>
        <td>${esc(p.nombre)}</td>
        <td>${esc(p.categoria)}</td>
        <td class="col-derecha">${p.stock}</td>
        <td class="col-derecha">${p.stockMinimo}</td>
        <td class="col-derecha">${formatoCOP(p.precio)}</td>
        <td>${pill}</td>
      </tr>`;
    }).join("") : `<tr><td colspan="7" class="col-centro">${inventario.length ? "Sin resultados." : "Aún no hay productos registrados."}</td></tr>`;
  }
})();
