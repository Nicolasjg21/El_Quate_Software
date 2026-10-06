/* ==========================================================================
   DASHBOARD.JS — Panel Principal con datos reales (solo administración)
   ========================================================================== */
(function () {
  const $ = (id) => document.getElementById(id);
  let inventario = [];

  const DIAS = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
  const inicioDia = (f) => { const d = new Date(f); d.setHours(0, 0, 0, 0); return d; };
  const claveDia = (f) => `${f.getFullYear()}-${f.getMonth()}-${f.getDate()}`;

  document.addEventListener("DOMContentLoaded", () => {
    const f = document.querySelector(".header-usuario-fecha");
    if (f) f.textContent = fechaHoy();
    $("buscar-panel-inventario").addEventListener("input", renderInventario);
    Auth.listo.then(cargar);
  });

  function tendencia(el, actual, previo, etiqueta) {
    if (!previo) {
      el.className = "kpi-tendencia";
      el.textContent = actual ? "Sin datos en los 7 días previos" : "Sin movimiento en 14 días";
      return;
    }
    const pct = Math.round(((actual - previo) / previo) * 100);
    el.className = "kpi-tendencia " + (pct >= 0 ? "kpi-tendencia--positiva" : "kpi-tendencia--negativa");
    el.textContent = `${pct >= 0 ? "↑" : "↓"} ${Math.abs(pct)}% vs 7 días previos`;
  }

  async function cargar() {
    const estado = $("estado-panel");
    estado.hidden = false;
    estado.textContent = "Cargando información...";

    const resultados = await Promise.allSettled([
      Negocio.ventas(), Negocio.ordenesPagadas(), Negocio.consumoPeriodo(), Negocio.productos()
    ]);
    const [rVentas, rOrdenes, rConsumo, rProductos] = resultados;
    const fallos = resultados.filter((r) => r.status === "rejected");
    if (fallos.length === resultados.length) {
      estado.textContent = Api.mensajeError(fallos[0].reason, "No fue posible cargar el panel. Intente de nuevo.");
      return;
    }
    estado.hidden = fallos.length === 0;
    if (fallos.length) estado.textContent = "Algunos datos no pudieron cargarse: " + Api.mensajeError(fallos[0].reason, "error de conexión");

    const ordenes = rOrdenes.status === "fulfilled" ? rOrdenes.value : [];
    const ventas = rVentas.status === "fulfilled" ? rVentas.value : null;

    /* KPIs */
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
    const enRango = (o, d, h) => { const t = new Date(o.fechaHora); return t >= d && t < h; };
    const sem = ordenes.filter((o) => enRango(o, hace7, fin));
    const prev = ordenes.filter((o) => enRango(o, hace14, hace7));
    const sumaSem = sem.reduce((a, o) => a + o.total, 0);
    const sumaPrev = prev.reduce((a, o) => a + o.total, 0);
    tendencia($("kpi-ventas-t"), sumaSem, sumaPrev, "Últimos 7 días");
    tendencia($("kpi-ordenes-t"), sem.length, prev.length, "Últimos 7 días");
    tendencia($("kpi-promedio-t"), sem.length ? sumaSem / sem.length : 0, prev.length ? sumaPrev / prev.length : 0, "Últimos 7 días");

    /* Inventario */
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

    dibujarTendencia(ordenes, hace7);

    if (rConsumo.status === "fulfilled") {
      dibujarCategorias(rConsumo.value.categorias);
      dibujarTop(rConsumo.value.productos.slice(0, 5));
    } else {
      $("tabla-top").innerHTML = `<tr><td colspan="4" class="col-centro">No fue posible cargar los productos más vendidos.</td></tr>`;
      $("chart-categorias").innerHTML = "";
    }
  }

  /* ---------- Gráfica de líneas (7 días) ---------- */
  function dibujarTendencia(ordenes, desde) {
    const ANCHO = 640, ALTO = 210;
    const dias = [];
    for (let i = 0; i < 7; i++) { const d = new Date(desde); d.setDate(d.getDate() + i); dias.push(d); }
    const ventas = new Map(dias.map((d) => [claveDia(d), 0]));
    const cuenta = new Map(dias.map((d) => [claveDia(d), 0]));
    ordenes.forEach((o) => {
      const k = claveDia(new Date(o.fechaHora));
      if (ventas.has(k)) { ventas.set(k, ventas.get(k) + o.total); cuenta.set(k, cuenta.get(k) + 1); }
    });
    const sv = dias.map((d) => ventas.get(claveDia(d)));
    const so = dias.map((d) => cuenta.get(claveDia(d)));

    const paso = (max) => {
      if (max <= 0) return 1;
      const mag = Math.pow(10, Math.floor(Math.log10(max)));
      const n = max / mag;
      return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * mag;
    };
    const maxV = paso(Math.max(...sv));
    const maxO = paso(Math.max(...so));

    $("chart-ejes").innerHTML = [4, 3, 2, 1, 0].map((i) => `<span>${i === 0 ? 0 : abreviar(maxV * i / 4)}</span>`).join("");
    $("chart-dias").innerHTML = dias.map((d) => `<span>${DIAS[d.getDay()]}</span>`).join("");

    const serie = (valores, max, clase) => {
      const pts = valores.map((v, i) => ({ x: (ANCHO / 6) * i, y: ALTO - (v / max) * ALTO, v }));
      let html = "";
      for (let i = 0; i < pts.length - 1; i++) {
        const a = pts[i], b = pts[i + 1];
        const dx = b.x - a.x, dy = b.y - a.y;
        const largo = Math.sqrt(dx * dx + dy * dy);
        const ang = Math.atan2(dy, dx) * 180 / Math.PI;
        html += `<div class="chart-segmento chart-segmento--${clase}" style="left:${a.x.toFixed(1)}px;top:${a.y.toFixed(1)}px;width:${largo.toFixed(1)}px;transform:rotate(${ang.toFixed(2)}deg);"></div>`;
      }
      pts.forEach((p, i) => {
        html += `<div class="chart-punto chart-punto--${clase}" style="left:${p.x.toFixed(1)}px;top:${p.y.toFixed(1)}px;" title="${esc(clase === "sales" ? formatoCOP(p.v) : p.v + " órdenes")}"></div>`;
      });
      return html;
    };
    $("chart-series").innerHTML = serie(so, maxO, "orders") + serie(sv, maxV, "sales");
  }

  function abreviar(n) {
    if (n >= 1e6) return (n / 1e6).toLocaleString("es-CO", { maximumFractionDigits: 1 }) + " M";
    if (n >= 1e3) return (n / 1e3).toLocaleString("es-CO", { maximumFractionDigits: 0 }) + " k";
    return String(Math.round(n));
  }

  function dibujarCategorias(cats) {
    const cont = $("chart-categorias");
    const top = cats.slice(0, 6);
    if (!top.length) { cont.innerHTML = `<p style="padding:20px;color:var(--texto-secundario);">Aún no hay ventas registradas.</p>`; return; }
    const max = Math.max(...top.map((c) => c.ingresos)) || 1;
    cont.innerHTML = top.map((c) => `
      <div class="chart-barra-col">
        <div class="chart-barra" style="height:${Math.max(3, (c.ingresos / max) * 85).toFixed(0)}%" title="${esc(c.nombre)}: ${esc(formatoCOP(c.ingresos))}"></div>
        <span class="chart-barra-etiqueta">${esc(c.nombre)}</span>
      </div>`).join("");
  }

  function dibujarTop(filas) {
    $("tabla-top").innerHTML = filas.length ? filas.map((f) => `
      <tr>
        <td>${esc(f.nombre)}</td>
        <td>${esc(f.categoria)}</td>
        <td class="col-derecha">${f.unidades.toLocaleString("es-CO")}</td>
        <td class="col-derecha">${formatoCOP(f.ingresos)}</td>
      </tr>`).join("") : `<tr><td colspan="4" class="col-centro">Aún no hay ventas registradas.</td></tr>`;
  }

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
