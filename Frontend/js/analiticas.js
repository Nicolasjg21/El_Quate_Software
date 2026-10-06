/* ==========================================================================
   ANALITICAS.JS — Ventas y Analíticas con datos reales (solo administración)
   --------------------------------------------------------------------------
   Fuentes (todas vía Negocio):
     · api/Analiticas/Ventas      → indicadores del período y del período anterior
     · api/Pedidos/Historial      → consumo por producto y categoría
     · api/Pedidos/filtrar        → órdenes pagadas (tendencia, horas, métodos, recientes)
   ========================================================================== */
(function () {
  const $ = (id) => document.getElementById(id);
  const DIAS = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
  const NOMBRE_PERIODO = {
    hoy: "Hoy", semana: "Esta semana", mes: "Este mes", ultimos30: "Últimos 30 días",
    anio: "Este año", todos: "Todos", personalizado: "Personalizado"
  };
  const COLORES_EXTRA = ["#E7B25A", "#5CB8B2", "#D97A9B", "#8C9AA8", "#C9A66B"];

  let ordenes = [];           // todas las órdenes pagadas (se cargan una vez)
  let ultimo = null;          // datos del último render (para exportar)
  let cargando = false;
  let solicitud = 0;

  const dia0 = (f) => { const d = new Date(f); d.setHours(0, 0, 0, 0); return d; };
  const dia24 = (f) => { const d = new Date(f); d.setHours(23, 59, 59, 0); return d; };
  const sumaDias = (f, n) => { const d = new Date(f); d.setDate(d.getDate() + n); return d; };
  const iso = (f) => `${f.getFullYear()}-${p2(f.getMonth() + 1)}-${p2(f.getDate())}`;
  const p2 = (n) => String(n).padStart(2, "0");
  const apiFecha = (f) => `${iso(f)}T${p2(f.getHours())}:${p2(f.getMinutes())}:${p2(f.getSeconds())}`;
  const fmtDia = (f) => `${p2(f.getDate())}/${p2(f.getMonth() + 1)}/${f.getFullYear()}`;
  const fmtDiaHora = (f) => `${fmtDia(f)} ${p2(f.getHours())}:${p2(f.getMinutes())}`;
  const pct = (n, d) => (d ? (n / d) * 100 : 0);
  const fmtPct = (n) => n.toLocaleString("es-CO", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%";
  const fmtNum = (n) => Math.round(n).toLocaleString("es-CO");

  document.addEventListener("DOMContentLoaded", () => {
    $("fecha-actual").textContent = fechaHoy();
    document.querySelectorAll('input[name="periodo"]').forEach((r) => r.addEventListener("change", () => {
      if (r.checked && r.id !== "per-personalizado") refrescar();
      if (r.checked && r.id === "per-personalizado") { inicializarRango(); refrescar(); }
    }));
    ["desde", "hasta"].forEach((id) => $(id).addEventListener("change", () => {
      if (periodoActual() === "personalizado") refrescar();
    }));
    $("btn-exportar").addEventListener("click", exportar);
    Auth.listo.then(() => cargarOrdenes().then(refrescar));
  });

  const periodoActual = () => {
    const r = document.querySelector('input[name="periodo"]:checked');
    return r ? r.id.replace("per-", "") : "semana";
  };

  function inicializarRango() {
    if (!$("desde").value) $("desde").value = iso(sumaDias(new Date(), -6));
    if (!$("hasta").value) $("hasta").value = iso(new Date());
  }

  /** Rango del período elegido y el rango anterior equivalente (null si no aplica). */
  function calcularRango(clave) {
    const ahora = new Date();
    const hoy0 = dia0(ahora);
    let desde, hasta = ahora, previo = null;
    switch (clave) {
      case "hoy": desde = hoy0; previo = [sumaDias(hoy0, -1), new Date(sumaDias(ahora, -1))]; break;
      case "semana": {
        const dif = (hoy0.getDay() + 6) % 7;
        desde = sumaDias(hoy0, -dif);
        previo = [sumaDias(desde, -7), sumaDias(ahora, -7)];
        break;
      }
      case "mes":
        desde = new Date(ahora.getFullYear(), ahora.getMonth(), 1);
        previo = [new Date(ahora.getFullYear(), ahora.getMonth() - 1, 1), new Date(ahora.getFullYear(), ahora.getMonth() - 1, Math.min(ahora.getDate(), new Date(ahora.getFullYear(), ahora.getMonth(), 0).getDate()), ahora.getHours(), ahora.getMinutes())];
        break;
      case "ultimos30": desde = sumaDias(hoy0, -29); previo = [sumaDias(desde, -30), sumaDias(desde, 0)]; previo[1] = new Date(desde.getTime() - 1000); break;
      case "anio":
        desde = new Date(ahora.getFullYear(), 0, 1);
        previo = [new Date(ahora.getFullYear() - 1, 0, 1), new Date(ahora.getFullYear() - 1, ahora.getMonth(), ahora.getDate(), ahora.getHours(), ahora.getMinutes())];
        break;
      case "todos": desde = null; break;
      default: {
        const d = $("desde").value, h = $("hasta").value;
        if (!d || !h) return { error: "Seleccione las fechas Desde y Hasta." };
        desde = new Date(d + "T00:00:00"); hasta = dia24(new Date(h + "T00:00:00"));
        if (desde > hasta) return { error: "La fecha Desde no puede ser posterior a la fecha Hasta." };
        const ms = hasta - desde + 1000;
        previo = [new Date(desde.getTime() - ms), new Date(desde.getTime() - 1000)];
      }
    }
    if (clave === "todos") {
      const fechas = ordenes.map((o) => new Date(o.fechaHora)).filter((f) => !isNaN(f));
      desde = fechas.length ? dia0(new Date(Math.min(...fechas))) : hoy0;
    }
    return { desde, hasta, previo, clave };
  }

  async function cargarOrdenes() {
    $("contenido").innerHTML = estado("carga", "&#8987;", "Cargando estadísticas...");
    try { ordenes = await Negocio.ordenesPagadas(); }
    catch (err) {
      $("contenido").innerHTML = estado("error", "&#9888;", Api.mensajeError(err, "No fue posible cargar las estadísticas. Intente nuevamente."));
      ordenes = [];
      return false;
    }
    return true;
  }

  function estado(tipo, icono, texto) {
    return `<article class="panel"><div class="estado estado--${tipo}"><span class="estado-icono">${icono}</span><p class="estado-titulo">${esc(texto)}</p></div></article>`;
  }

  async function refrescar() {
    if (cargando) { solicitud++; }
    const miSolicitud = ++solicitud;
    const clave = periodoActual();
    const rango = calcularRango(clave);
    $("ctx").innerHTML = "";
    if (rango.error) { $("contenido").innerHTML = estado("vacio", "&#128202;", rango.error); ultimo = null; return; }

    $("ctx").innerHTML = `<p><span class="ctx-et">Mostrando</span><strong>${esc(NOMBRE_PERIODO[clave])}</strong> · ${fmtDia(rango.desde)}${iso(rango.desde) === iso(rango.hasta) ? "" : " – " + fmtDia(rango.hasta)}</p>` +
      (rango.previo ? `<p><span class="ctx-et">Comparado con</span>${fmtDia(rango.previo[0])}${iso(rango.previo[0]) === iso(rango.previo[1]) ? "" : " – " + fmtDia(rango.previo[1])}</p>` : "");
    $("contenido").innerHTML = estado("carga", "&#8987;", "Cargando estadísticas...");
    cargando = true;

    const d = clave === "todos" ? null : apiFecha(rango.desde);
    const h = clave === "todos" ? null : apiFecha(rango.hasta);
    const pedidos = [
      Negocio.ventas(d, h),
      Negocio.consumoPeriodo(d, h),
      rango.previo ? Negocio.ventas(apiFecha(rango.previo[0]), apiFecha(rango.previo[1])) : Promise.resolve(null)
    ];
    let res;
    try { res = await Promise.all(pedidos); }
    catch (err) {
      if (miSolicitud !== solicitud) return;
      cargando = false;
      $("contenido").innerHTML = estado("error", "&#9888;", Api.mensajeError(err, "No fue posible cargar las estadísticas. Intente nuevamente."));
      ultimo = null;
      return;
    }
    if (miSolicitud !== solicitud) return;
    cargando = false;

    const [ventas, consumo, ventasPrev] = res;
    const enPeriodo = ordenes.filter((o) => { const t = new Date(o.fechaHora); return t >= rango.desde && t <= rango.hasta; });
    if (!ventas.totalVentas && !enPeriodo.length) {
      $("contenido").innerHTML = estado("vacio", "&#128202;", "No hay datos suficientes para mostrar estadísticas en este período.");
      ultimo = null;
      return;
    }
    ultimo = { clave, rango, ventas, ventasPrev, consumo, enPeriodo };
    $("contenido").innerHTML = construir(ultimo);
  }

  /* ---------- Construcción del contenido ---------- */
  function variacion(actual, previo) {
    if (previo === null || previo === undefined) return `<p class="kpi-tend kpi-tend--nd">Sin período de comparación</p>`;
    if (!previo) return `<p class="kpi-tend kpi-tend--nd">${actual ? "Sin datos en el período anterior" : "Sin cambios"}</p>`;
    const v = ((actual - previo) / previo) * 100;
    const clase = v > 0 ? "pos" : v < 0 ? "neg" : "nd";
    const flecha = v > 0 ? "&#8593;" : v < 0 ? "&#8595;" : "=";
    return `<p class="kpi-tend kpi-tend--${clase}">${flecha} ${fmtPct(Math.abs(v))} vs período anterior</p>`;
  }

  function construir(d) {
    const { ventas, ventasPrev, consumo, enPeriodo } = d;
    const p = ventasPrev;
    const kpi = (titulo, icono, valor, actual, previo) =>
      `<article class="kpi"><div class="kpi-fila"><p class="kpi-titulo">${titulo}</p><span class="kpi-icono" aria-hidden="true">${icono}</span></div><p class="kpi-valor">${valor}</p>${variacion(actual, previo)}</article>`;

    const kpis = `<section class="kpis" aria-label="Indicadores principales">` +
      kpi("Ventas Totales", "$", formatoCOP(ventas.totalIngresos), ventas.totalIngresos, p ? p.totalIngresos : null) +
      kpi("Órdenes Totales", "&#128722;", fmtNum(ventas.totalVentas), ventas.totalVentas, p ? p.totalVentas : null) +
      kpi("Valor Promedio de Orden", "&#128200;", formatoCOP(ventas.promedioVenta), ventas.promedioVenta, p ? p.promedioVenta : null) +
      kpi("Productos Vendidos", "&#127866;", fmtNum(ventas.totalProductosVendidos), ventas.totalProductosVendidos, p ? p.totalProductosVendidos : null) +
      `</section><p class="nota">Ventas = total cobrado en los comprobantes del período (incluye IVA). Las variaciones comparan contra el tramo equivalente del período anterior.</p>`;

    return kpis + panelTendencia(d) +
      `<div class="fila fila--b">${panelCategorias(d)}${panelProductos(d)}</div>` +
      `<div class="fila fila--c">${panelHoras(d)}${panelPagos(d)}</div>` +
      panelRecientes(d);
  }

  /* ---------- Escala "bonita" ---------- */
  function escala(max, divisiones) {
    if (max <= 0) return { tope: divisiones, paso: 1 };
    const crudo = max / divisiones;
    const mag = Math.pow(10, Math.floor(Math.log10(crudo)));
    const n = crudo / mag;
    const paso = (n <= 1 ? 1 : n <= 2 ? 2 : n <= 2.5 ? 2.5 : n <= 5 ? 5 : 10) * mag;
    return { tope: paso * divisiones, paso };
  }
  function abreviarCOP(n) {
    if (n >= 1e6) return "$ " + (n / 1e6).toLocaleString("es-CO", { maximumFractionDigits: 1 }) + " M";
    if (n >= 1e3) return "$ " + (n / 1e3).toLocaleString("es-CO", { maximumFractionDigits: 1 }) + " mil";
    return "$ " + Math.round(n);
  }

  /* ---------- Tendencia ---------- */
  function cubos(rango, clave) {
    const lista = [];
    const dias = Math.round((dia0(rango.hasta) - dia0(rango.desde)) / 86400000) + 1;
    if (dias <= 1) {
      const base = dia0(rango.desde);
      const horaTope = iso(base) === iso(new Date()) ? new Date().getHours() : 23;
      for (let h = 0; h <= 23; h++) {
        const a = new Date(base); a.setHours(h, 0, 0, 0);
        const b = new Date(base); b.setHours(h, 59, 59, 999);
        lista.push({ a, b, etq: `${p2(h)}:00`, tip: `${fmtDia(base)} ${p2(h)}:00 – ${p2(h)}:59`, fut: h > horaTope, parcial: h === horaTope && iso(base) === iso(new Date()) });
      }
    } else if (dias <= 92) {
      for (let i = 0; i < dias; i++) {
        const dia = sumaDias(dia0(rango.desde), i);
        const hoy = iso(dia) === iso(new Date());
        lista.push({ a: dia0(dia), b: dia24(dia), etq: `${DIAS[dia.getDay()]} <b>${dia.getDate()}</b>`, tip: `${DIAS[dia.getDay()]} ${fmtDia(dia)}`, fut: dia > new Date(), parcial: hoy });
      }
    } else {
      const ini = new Date(rango.desde.getFullYear(), rango.desde.getMonth(), 1);
      for (let m = new Date(ini); m <= rango.hasta; m = new Date(m.getFullYear(), m.getMonth() + 1, 1)) {
        const fin = new Date(m.getFullYear(), m.getMonth() + 1, 0, 23, 59, 59);
        const nombre = m.toLocaleDateString("es-CO", { month: "short", year: "2-digit" });
        lista.push({ a: m, b: fin, etq: esc(nombre), tip: m.toLocaleDateString("es-CO", { month: "long", year: "numeric" }), fut: false, parcial: m.getMonth() === new Date().getMonth() && m.getFullYear() === new Date().getFullYear() });
      }
    }
    return lista;
  }

  function panelTendencia(d) {
    const cs = cubos(d.rango, d.clave);
    cs.forEach((c) => { c.ventas = 0; c.ords = 0; });
    d.enPeriodo.forEach((o) => {
      const t = new Date(o.fechaHora);
      const c = cs.find((x) => t >= x.a && t <= x.b);
      if (c) { c.ventas += o.total; c.ords += 1; }
    });
    const n = cs.length;
    const ev = escala(Math.max(...cs.map((c) => c.ventas), 0), 5);
    const eo = escala(Math.max(...cs.map((c) => c.ords), 0), 5);
    const x = (i) => ((i + 0.5) / n) * 100;
    const yv = (v) => 100 - (v / ev.tope) * 100;
    const yo = (v) => 100 - (v / eo.tope) * 100;

    const visibles = cs.map((c, i) => ({ c, i })).filter(({ c }) => !c.fut);
    const pts = (fn) => visibles.map(({ c, i }) => `${x(i).toFixed(2)},${fn(c).toFixed(2)}`).join(" ");
    const ult = visibles.length - 1;
    const hayParcial = ult >= 0 && visibles[ult].c.parcial;
    const solidos = hayParcial ? visibles.slice(0, ult) : visibles;
    const ptsDe = (lista, fn) => lista.map(({ c, i }) => `${x(i).toFixed(2)},${fn(c).toFixed(2)}`).join(" ");
    const tramo = (clase, fn, extra) => {
      let h = solidos.length > 1 ? `<polyline class="${clase}" points="${ptsDe(solidos, fn)}"/>` : "";
      if (hayParcial && ult >= 1) h += `<polyline class="${clase} parcial" points="${ptsDe(visibles.slice(ult - 1), fn)}"/>`;
      return h;
    };
    const area = visibles.length > 1
      ? `<polygon class="area" points="${x(visibles[0].i).toFixed(2)},100 ${pts(((c) => yv(c.ventas)))} ${x(visibles[ult].i).toFixed(2)},100"/>` : "";
    const svg = `<svg viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">${area}${tramo("l-o", (c) => yo(c.ords))}${tramo("l-v", (c) => yv(c.ventas))}</svg>`;

    const guias = [0, 20, 40, 60, 80, 100].map((t) => `<i style="top:${t}%"></i>`).join("");
    const ancho = 100 / n;
    const zonas = cs.map((c, i) => {
      if (c.fut) return "";
      const lado = i < n * 0.25 ? "izq" : i > n * 0.75 ? "der" : "cen";
      return `<div class="tc tc--${lado}" tabindex="0" style="left:${(i * ancho).toFixed(3)}%;width:${ancho.toFixed(3)}%"><i class="tc-g"></i>` +
        `<b class="mk mk-v${c.parcial ? " parcial" : ""}" style="top:${yv(c.ventas).toFixed(2)}%"></b><b class="mk mk-o${c.parcial ? " parcial" : ""}" style="top:${yo(c.ords).toFixed(2)}%"></b>` +
        `<div class="tc-tip"><strong>${esc(c.tip)}</strong><span><i class="lg lg-v"></i>Ventas: ${formatoCOP(c.ventas)}</span><span><i class="lg lg-o"></i>Órdenes: ${c.ords}</span>${c.parcial ? "<em>Dato parcial: el período sigue en curso</em>" : ""}</div></div>`;
    }).join("");

    const ejeV = [0, 1, 2, 3, 4, 5].map((k) => `<span style="top:${(100 - k * 20).toFixed(3)}%">${k === 0 ? "$ 0" : abreviarCOP(ev.paso * k)}</span>`).join("");
    const ejeO = [0, 1, 2, 3, 4, 5].map((k) => `<span style="top:${(100 - k * 20).toFixed(3)}%">${Number((eo.paso * k).toFixed(1)).toLocaleString("es-CO")}</span>`).join("");
    const salto = Math.ceil(n / 12);
    const etqs = cs.map((c, i) => (i % salto === 0 ? `<span class="${c.fut ? "fut" : ""}" style="left:${x(i).toFixed(2)}%">${c.etq}</span>` : "")).join("");
    const pie = n === 24 ? "Hora del día" : cs.length && d.rango && (dia0(d.rango.hasta) - dia0(d.rango.desde)) / 86400000 + 1 > 92 ? "Mes" : "Día";

    return `<article class="panel panel--tendencia"><div class="panel-cab"><div><h2 class="panel-titulo">Tendencia de Ventas y Órdenes</h2><p class="panel-desc">Ventas en pesos (COP) y órdenes pagadas${n === 24 ? " por hora" : n > 31 && pie === "Mes" ? " por mes" : " por día"}</p></div>` +
      `<div class="leyenda"><span class="ley"><i class="lg lg-v"></i>Ventas (COP)</span><span class="ley"><i class="lg lg-o"></i>Órdenes</span><span class="ley"><i class="lg lg-p"></i>Dato parcial</span></div></div>` +
      `<div class="trend ${n <= 31 ? "trend--pts" : ""}"><span class="eje-tit eje-tit--v">Ventas (COP)</span><span></span><span class="eje-tit eje-tit--o">Órdenes</span>` +
      `<div class="eje eje--v" aria-hidden="true">${ejeV}</div><div class="plot" role="group" aria-label="Gráfica de ventas y órdenes">${guias}${svg}${zonas}</div><div class="eje eje--o" aria-hidden="true">${ejeO}</div>` +
      `<span></span><div class="xl${n > 14 ? " xl--rala" : ""}" aria-hidden="true">${etqs}</div><span></span></div><p class="eje-cap">${pie}</p></article>`;
  }

  /* ---------- Categorías ---------- */
  function panelCategorias(d) {
    const cats = d.consumo.categorias;
    const total = cats.reduce((a, c) => a + c.ingresos, 0);
    const max = cats.length ? cats[0].ingresos : 0;
    const lista = cats.length ? `<ul class="cats">` + cats.map((c, i) =>
      `<li class="cat ${i === 0 ? "top" : ""}"><div class="cat-linea"><span class="cat-nombre">${esc(c.nombre)}</span><span class="cat-valor">${formatoCOP(c.ingresos)}</span></div>` +
      `<div class="cat-barra"><div class="pista"><i style="width:${pct(c.ingresos, max).toFixed(1)}%"></i></div><span class="cat-pct">${fmtPct(pct(c.ingresos, total))}</span></div></li>`).join("") + `</ul>`
      : `<div class="estado estado--vacio"><p class="estado-titulo">Sin consumo registrado en el período.</p></div>`;
    return `<article class="panel panel--cat"><div class="panel-cab"><div><h2 class="panel-titulo">Ingresos por Categoría</h2><p class="panel-desc">Consumo por categoría del catálogo (COP, antes de IVA)</p></div><span class="panel-per">${esc(NOMBRE_PERIODO[d.clave])}</span></div>${lista}` +
      `<div class="cats-total"><span>Total consumo</span><strong>${formatoCOP(total)}</strong></div></article>`;
  }

  /* ---------- Productos ---------- */
  function panelProductos(d) {
    const prods = d.consumo.productos;
    const total = prods.reduce((a, f) => a + f.ingresos, 0);
    const top = prods.slice(0, 10);
    const resto = prods.slice(10);
    const maxIng = top.length ? Math.max(...top.map((f) => f.ingresos)) : 0;
    const filas = top.map((f, i) =>
      `<tr><td class="col-rk">${i + 1}</td><td class="col-prod">${esc(f.nombre)}</td><td class="col-cat"><span class="tag">${esc(f.categoria)}</span></td><td class="col-derecha">${fmtNum(f.unidades)}</td><td class="col-derecha">${formatoCOP(f.ingresos)}</td>` +
      `<td class="col-part"><div class="part"><span>${fmtPct(pct(f.ingresos, total))}</span><div class="pista pista--mini"><i style="width:${pct(f.ingresos, maxIng).toFixed(1)}%"></i></div></div></td></tr>`).join("");
    const otros = resto.length ? `<tr class="fila-otros"><td></td><td>Otros (${resto.length})</td><td></td><td class="col-derecha">${fmtNum(resto.reduce((a, f) => a + f.unidades, 0))}</td><td class="col-derecha">${formatoCOP(resto.reduce((a, f) => a + f.ingresos, 0))}</td><td class="col-part"><div class="part"><span>${fmtPct(pct(resto.reduce((a, f) => a + f.ingresos, 0), total))}</span></div></td></tr>` : "";
    const tot = prods.length ? `<tr class="fila-total"><td></td><td>Total</td><td></td><td class="col-derecha">${fmtNum(prods.reduce((a, f) => a + f.unidades, 0))}</td><td class="col-derecha">${formatoCOP(total)}</td><td class="col-part"><div class="part"><span>100,0%</span></div></td></tr>` : "";
    const cuerpo = prods.length ? filas + otros + tot : `<tr><td colspan="6" class="col-centro">Sin productos vendidos en el período.</td></tr>`;
    return `<article class="panel panel--prod"><div class="panel-cab"><div><h2 class="panel-titulo">Productos Más Vendidos</h2><p class="panel-desc">Ordenados por unidades vendidas en órdenes pagadas</p></div><span class="panel-per">${esc(NOMBRE_PERIODO[d.clave])}</span></div>` +
      `<div class="tabla-scroll"><table class="tabla tabla--prod"><thead><tr><th class="col-rk">#</th><th>Producto</th><th class="col-cat">Categoría</th><th class="col-derecha"><span class="h-largo">Unidades Vendidas</span><span class="h-corto">Unid.</span></th><th class="col-derecha">Ingresos</th><th class="col-part">Participación</th></tr></thead><tbody>${cuerpo}</tbody></table></div></article>`;
  }

  /* ---------- Actividad por hora ---------- */
  function panelHoras(d) {
    const horas = Array.from({ length: 24 }, () => ({ n: 0, v: 0 }));
    d.enPeriodo.forEach((o) => { const h = new Date(o.fechaHora).getHours(); horas[h].n++; horas[h].v += o.total; });
    const max = Math.max(...horas.map((h) => h.n), 0);
    const e = escala(max, 6);
    const pico = max > 0 ? horas.findIndex((h) => h.n === max) : -1;
    const eje = [0, 1, 2, 3, 4, 5, 6].map((k) => `<span style="top:${(100 - (k * 100) / 6).toFixed(3)}%">${Number((e.paso * k).toFixed(1)).toLocaleString("es-CO")}</span>`).join("");
    const guias = [0, 1, 2, 3, 4, 5, 6].map((k) => `<i style="top:${((k * 100) / 6).toFixed(3)}%"></i>`).join("");
    const cols = horas.map((h, i) => {
      const lado = i < 6 ? "izq" : i > 17 ? "der" : "cen";
      return `<div class="hc hc--${lado}${i === pico ? " hc--pico" : ""}" tabindex="0"><div class="tc-tip"><strong>${p2(i)}:00 – ${p2(i)}:59</strong><span>Órdenes: ${h.n}</span><span>Ventas: ${formatoCOP(h.v)}</span></div>` +
        `<div class="hc-area">${i === pico ? `<span class="hc-val">${h.n}</span>` : ""}<div class="hc-bar" style="height:${(h.n / e.tope * 100).toFixed(1)}%"></div></div><span class="hc-lbl">${p2(i)}</span></div>`;
    }).join("");
    return `<article class="panel panel--horas"><div class="panel-cab"><div><h2 class="panel-titulo">Actividad por Hora</h2><p class="panel-desc">Órdenes pagadas según la hora del día</p></div><span class="panel-per">${esc(NOMBRE_PERIODO[d.clave])}</span></div>` +
      `<div class="horas"><div class="eje eje--v" aria-hidden="true">${eje}</div><div class="horas-plot"><div class="hg">${guias}</div><div class="horas-cols">${cols}</div></div></div>` +
      `<p class="eje-cap">Hora del día (24 h)${pico >= 0 ? " · barra destacada: hora con más órdenes" : ""}</p></article>`;
  }

  /* ---------- Métodos de pago ---------- */
  function claseMetodo(nombre) {
    const n = (nombre || "").toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "");
    if (n.startsWith("efectivo")) return "efectivo";
    if (n.startsWith("tarjeta")) return "tarjeta";
    if (n.startsWith("billetera")) return "billetera";
    return "otro";
  }

  function panelPagos(d) {
    const grupos = new Map();
    d.enPeriodo.forEach((o) => {
      const g = grupos.get(o.metodoPago) || { nombre: o.metodoPago, n: 0, v: 0 };
      g.n++; g.v += o.total; grupos.set(o.metodoPago, g);
    });
    const lista = [...grupos.values()].sort((a, b) => b.n - a.n);
    const total = lista.reduce((a, g) => a + g.n, 0);
    let extra = 0;
    lista.forEach((g) => {
      g.clase = claseMetodo(g.nombre);
      g.color = g.clase === "efectivo" ? "var(--c-efectivo)" : g.clase === "tarjeta" ? "var(--c-tarjeta)" : g.clase === "billetera" ? "var(--c-billetera)" : COLORES_EXTRA[extra++ % COLORES_EXTRA.length];
    });
    let acum = 0;
    const tramos = lista.map((g) => { const ini = acum; acum += pct(g.n, total); return `${g.color} ${ini.toFixed(3)}% ${acum.toFixed(3)}%`; }).join(", ");
    const items = lista.map((g) =>
      `<li class="pago pago--${g.clase}"><span class="pago-punto" style="background-color:${g.color}"></span><div class="pago-info"><strong>${esc(g.nombre)}</strong><span>${g.n} ${g.n === 1 ? "orden" : "órdenes"} · ${fmtPct(pct(g.n, total))}</span></div><span class="pago-valor">${formatoCOP(g.v)}</span></li>`).join("");
    const cuerpo = total
      ? `<div class="pagos"><div class="donut" style="background:conic-gradient(${tramos})" role="img" aria-label="Distribución de métodos de pago"><div class="donut-centro"><strong>${total}</strong><span>órdenes</span></div></div><ul class="pagos-lista">${items}</ul></div>`
      : `<div class="estado estado--vacio"><p class="estado-titulo">Sin órdenes pagadas en el período.</p></div>`;
    return `<article class="panel panel--pagos"><div class="panel-cab"><div><h2 class="panel-titulo">Métodos de Pago</h2><p class="panel-desc">Distribución de las órdenes pagadas</p></div><span class="panel-per">${esc(NOMBRE_PERIODO[d.clave])}</span></div>${cuerpo}</article>`;
  }

  /* ---------- Recientes ---------- */
  const ICONO_OJO = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/></svg>`;

  function panelRecientes(d) {
    const rec = [...d.enPeriodo].sort((a, b) => String(b.fechaHora).localeCompare(String(a.fechaHora))).slice(0, 10);
    const filas = rec.map((o) => {
      const consumo = Math.round(o.total / (1 + Negocio.IVA));
      return `<tr><td class="col-mono">${esc(o.id)}</td><td class="col-fecha">${fmtDiaHora(new Date(o.fechaHora))}</td><td class="col-centro">${esc(o.mesa)}</td><td>${esc(o.mesero)}</td>` +
        `<td class="col-derecha">${formatoCOP(consumo)}</td><td class="col-derecha">${formatoCOP(o.total)}</td>` +
        `<td class="col-centro"><span class="metodo-pago metodo-pago--${claseMetodo(o.metodoPago)}"><span class="metodo-pago-punto"></span>${esc(o.metodoPago)}</span></td>` +
        `<td class="col-centro"><a class="accion-ver" href="historiales.html" aria-label="Ver la orden ${esc(o.id)} en Historiales">${ICONO_OJO}</a></td></tr>`;
    }).join("");
    return `<article class="panel panel--rec"><div class="panel-cab"><div><h2 class="panel-titulo">Órdenes Pagadas Recientes</h2><p class="panel-desc">Últimas órdenes del período · Consumo = total sin IVA</p></div><a class="btn btn--secundario btn--chico" href="historiales.html">Ver todas en Historiales</a></div>` +
      `<div class="tabla-scroll"><table class="tabla tabla--rec"><thead><tr><th>ID Orden</th><th>Fecha y Hora</th><th class="col-centro">Mesa</th><th>Mesero</th><th class="col-derecha">Consumo</th><th class="col-derecha">Total Pagado</th><th class="col-centro">Método de Pago</th><th class="col-centro">Ver</th></tr></thead>` +
      `<tbody>${filas || `<tr><td colspan="8" class="col-centro">Sin órdenes en el período.</td></tr>`}</tbody></table></div><p class="tabla-pie">Mostrando ${rec.length} de ${d.enPeriodo.length} órdenes del período</p></article>`;
  }

  function aviso(texto) {
    const t = $("toast");
    t.textContent = texto;
    t.classList.add("toast--visible");
    clearTimeout(aviso._t);
    aviso._t = setTimeout(() => t.classList.remove("toast--visible"), 2500);
  }

  /* ---------- Exportar CSV ---------- */
  function exportar() {
    if (!ultimo) { aviso("No hay datos para exportar en este período."); return; }
    const u = ultimo;
    const q = (v) => `"${String(v === undefined || v === null ? "" : v).replace(/"/g, '""')}"`;
    const fila = (...c) => c.map(q).join(";");
    const l = [];
    l.push(fila("Ventas y Analíticas - El Cuate"));
    l.push(fila("Período", NOMBRE_PERIODO[u.clave]));
    l.push(fila("Rango", `${fmtDia(u.rango.desde)} - ${fmtDia(u.rango.hasta)}`));
    l.push("");
    l.push(fila("Indicador", "Valor"));
    l.push(fila("Ventas Totales (COP)", Math.round(u.ventas.totalIngresos)));
    l.push(fila("Órdenes Totales", u.ventas.totalVentas));
    l.push(fila("Valor Promedio de Orden (COP)", Math.round(u.ventas.promedioVenta)));
    l.push(fila("Productos Vendidos", u.ventas.totalProductosVendidos));
    l.push("");
    l.push(fila("Categoría", "Consumo (COP)"));
    u.consumo.categorias.forEach((c) => l.push(fila(c.nombre, Math.round(c.ingresos))));
    l.push("");
    l.push(fila("Producto", "Categoría", "Unidades", "Ingresos (COP)"));
    u.consumo.productos.forEach((f) => l.push(fila(f.nombre, f.categoria, f.unidades, Math.round(f.ingresos))));
    l.push("");
    l.push(fila("ID Orden", "Fecha y Hora", "Mesa", "Mesero", "Total Pagado (COP)", "Método de Pago"));
    u.enPeriodo.forEach((o) => l.push(fila(o.id, fmtDiaHora(new Date(o.fechaHora)), o.mesa, o.mesero, Math.round(o.total), o.metodoPago)));
    const blob = new Blob(["﻿" + l.join("\r\n")], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url; a.download = `analiticas-${u.clave}.csv`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
})();
