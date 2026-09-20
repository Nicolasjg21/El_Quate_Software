/* ==========================================================================
   UI — utilidades compartidas (sin dependencias)
   ========================================================================== */
(function (App) {
  "use strict";

  var ui = {};

  /* ---------- Seguridad: TODO dato de la BD que se pinte pasa por esc() ---------- */
  ui.esc = function (valor) {
    return String(valor == null ? "" : valor)
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
  };

  /* ---------- Atajos DOM ---------- */
  ui.$ = function (selector, raiz) { return (raiz || document).querySelector(selector); };
  ui.$$ = function (selector, raiz) { return Array.prototype.slice.call((raiz || document).querySelectorAll(selector)); };
  ui.valor = function (id) { var e = document.getElementById(id); return e ? e.value : ""; };
  ui.poner = function (id, v) { var e = document.getElementById(id); if (e) e.value = (v == null ? "" : v); };
  ui.html = function (id, contenido) { var e = document.getElementById(id); if (e) e.innerHTML = contenido; };
  ui.texto = function (id, contenido) { var e = document.getElementById(id); if (e) e.textContent = contenido; };
  ui.mostrar = function (id, visible) { var e = document.getElementById(id); if (e) e.classList.toggle("oculto", !visible); };

  /* Delegación de eventos: un solo listener por tabla en vez de onclick="" con
     ids concatenados dentro del HTML (que además permitía inyección de código). */
  ui.alHacerClic = function (contenedor, selector, manejador) {
    var raiz = typeof contenedor === "string" ? document.getElementById(contenedor) : contenedor;
    if (!raiz) return;
    raiz.addEventListener("click", function (ev) {
      var objetivo = ev.target.closest(selector);
      if (objetivo && raiz.contains(objetivo)) manejador(objetivo, ev);
    });
  };

  ui.debounce = function (fn, ms) {
    var t;
    return function () { var a = arguments, c = this; clearTimeout(t); t = setTimeout(function () { fn.apply(c, a); }, ms || 200); };
  };

  /* ---------- Números y monedas ---------- */
  var fmtNumero = new Intl.NumberFormat("es-CO", { maximumFractionDigits: 2 });
  ui.num = function (n) { return fmtNumero.format(Number(n) || 0); };
  ui.moneda = function (n) { return "$ " + ui.num(n); };
  ui.redondear = function (n) { return Math.round((Number(n) + Number.EPSILON) * 100) / 100; };

  /* ---------- Fechas ----------
     El backend devuelve DateTime SIN zona ("2026-09-19T14:30:00.1234567").
     Se interpreta como hora local, y al ENVIAR se manda hora local SIN "Z"
     (toISOString() enviaba UTC y corría la hora 5 h en Colombia). */
  ui.parseFecha = function (valor) {
    if (!valor) return null;
    var s = String(valor).replace(/(\.\d{3})\d+/, "$1");
    var d = new Date(s);
    return isNaN(d.getTime()) ? null : d;
  };
  function dos(n) { return (n < 10 ? "0" : "") + n; }
  ui.fecha = function (valor) {
    var d = ui.parseFecha(valor);
    return d ? dos(d.getDate()) + "/" + dos(d.getMonth() + 1) + "/" + d.getFullYear() + " " + dos(d.getHours()) + ":" + dos(d.getMinutes()) : "—";
  };
  ui.soloFecha = function (valor) {
    var d = ui.parseFecha(valor);
    return d ? dos(d.getDate()) + "/" + dos(d.getMonth() + 1) + "/" + d.getFullYear() : "—";
  };
  ui.hora = function (valor) {
    var d = ui.parseFecha(valor);
    return d ? dos(d.getHours()) + ":" + dos(d.getMinutes()) : "—";
  };
  ui.ahoraLocal = function () {
    var d = new Date();
    return d.getFullYear() + "-" + dos(d.getMonth() + 1) + "-" + dos(d.getDate()) + "T" +
           dos(d.getHours()) + ":" + dos(d.getMinutes()) + ":" + dos(d.getSeconds());
  };
  ui.inicioDelDia = function (d) { var x = new Date(d || Date.now()); x.setHours(0, 0, 0, 0); return x; };

  /* ---------- Mensajes ---------- */
  var temporizadorAviso = null;
  ui.aviso = function (texto, tipo) {
    var caja = document.getElementById("mensaje");
    if (!caja) { (tipo === "error" ? console.error : console.log)(texto); return; }
    caja.textContent = texto;
    caja.className = "mensaje " + (tipo || "exito");
    clearTimeout(temporizadorAviso);
    if (tipo !== "error") temporizadorAviso = setTimeout(function () { caja.classList.add("oculto"); }, 5000);
    if (typeof caja.scrollIntoView === "function") caja.scrollIntoView({ block: "nearest" });
  };
  ui.ocultarAviso = function () { var c = document.getElementById("mensaje"); if (c) c.classList.add("oculto"); };
  ui.mensajeError = function (e) { return (e && e.message) ? e.message : "Ocurrió un error inesperado."; };

  /* Mensaje que sobrevive a una redirección (p. ej. "Pago registrado" → mesas). */
  ui.flash = function (texto, tipo) {
    try { window.sessionStorage.setItem("cuate.flash", JSON.stringify({ texto: texto, tipo: tipo || "exito" })); } catch (e) { /* nada */ }
  };
  ui.mostrarFlash = function () {
    try {
      var bruto = window.sessionStorage.getItem("cuate.flash");
      if (!bruto) return;
      window.sessionStorage.removeItem("cuate.flash");
      var f = JSON.parse(bruto);
      ui.aviso(f.texto, f.tipo);
    } catch (e) { /* nada */ }
  };

  /* ---------- Piezas de HTML ---------- */
  ui.badge = function (texto, color) {
    return "<span class='badge " + ui.esc(color || "gris") + "'>" + ui.esc(texto) + "</span>";
  };
  ui.opciones = function (items, valorClave, textoFn, seleccionado, textoVacio) {
    var h = textoVacio != null ? "<option value=''>" + ui.esc(textoVacio) + "</option>" : "";
    items.forEach(function (it) {
      var v = it[valorClave];
      h += "<option value='" + ui.esc(v) + "'" + (String(v) === String(seleccionado) ? " selected" : "") + ">" +
           ui.esc(typeof textoFn === "function" ? textoFn(it) : it[textoFn]) + "</option>";
    });
    return h;
  };
  /* Filas de tabla o, si no hay ninguna, un estado vacío con icono y (opcional) botón de acción. */
  ui.filasOVacio = function (filas, columnas, textoVacio, opciones) {
    if (filas.length) return filas.join("");
    var o = opciones || {};
    return "<tr><td colspan='" + columnas + "' class='vacio-celda'><span class='icono-grande'>" + (o.icono || "📭") + "</span>" +
           ui.esc(textoVacio) + (o.accion ? "<div style='margin-top:12px'>" + o.accion + "</div>" : "") + "</td></tr>";
  };
  /* Bloque de estado vacío para tarjetas (no tablas). */
  ui.vacio = function (icono, titulo, texto, accionHtml) {
    return "<div class='estado-vacio'><div class='icono-grande'>" + icono + "</div><h4>" + ui.esc(titulo) + "</h4>" +
           (texto ? "<p>" + ui.esc(texto) + "</p>" : "") + (accionHtml || "") + "</div>";
  };
  /* "hace 5 min", "hace 2 h", "hace 3 días" */
  ui.hace = function (valor) {
    var d = ui.parseFecha(valor);
    if (!d) return "—";
    var min = Math.max(0, Math.round((Date.now() - d.getTime()) / 60000));
    if (min < 1) return "ahora";
    if (min < 60) return "hace " + min + " min";
    if (min < 1440) return "hace " + Math.floor(min / 60) + " h " + (min % 60) + " min";
    return "hace " + Math.floor(min / 1440) + " d";
  };
  ui.indexar = function (lista, clave) {
    var m = new Map();
    (lista || []).forEach(function (x) { m.set(x[clave], x); });
    return m;
  };
  ui.nombreCompleto = function (u) { return u ? (String(u.nombres || "") + " " + String(u.apellidos || "")).trim() : "—"; };

  /* ---------- Validación reutilizable ----------
     ui.validar([{ id, etiqueta, requerido, tipo, min, max, largoMin, largoMax, patron, mensaje }])
     tipos: texto | entero | decimal | email | telefono | documento | select | password
     Marca los campos con la clase .invalido y devuelve { ok, errores[], valores{} } */
  var RE_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
  var RE_TEL = /^[0-9+\-\s()]{7,20}$/;
  var RE_DOC = /^[A-Za-z0-9\-]{4,20}$/;

  ui.limpiarErrores = function (ids) {
    (ids || []).forEach(function (id) {
      var e = document.getElementById(id);
      if (e) { e.classList.remove("invalido"); e.removeAttribute("aria-invalid"); }
    });
  };

  ui.validar = function (reglas) {
    var errores = [], valores = {};
    ui.limpiarErrores(reglas.map(function (r) { return r.id; }));

    reglas.forEach(function (r) {
      var el = document.getElementById(r.id);
      var crudo = el ? (el.type === "checkbox" ? el.checked : String(el.value)) : "";
      var texto = typeof crudo === "string" ? crudo.trim() : crudo;
      var msg = null;
      var etq = r.etiqueta || r.id;

      if (r.tipo === "checkbox") { valores[r.id] = !!crudo; return; }

      if (texto === "" || texto == null) {
        if (r.requerido) msg = etq + " es obligatorio.";
        valores[r.id] = "";
      } else if (r.tipo === "entero" || r.tipo === "decimal") {
        var n = Number(texto);
        if (!isFinite(n)) msg = etq + " debe ser un número.";
        else if (r.tipo === "entero" && Math.floor(n) !== n) msg = etq + " debe ser un número entero.";
        else if (r.tipo === "decimal" && Math.abs(n * 100 - Math.round(n * 100)) > 1e-6) msg = etq + " admite máximo 2 decimales.";
        else if (r.min != null && n < r.min) msg = etq + " debe ser mayor o igual a " + r.min + ".";
        else if (r.max != null && n > r.max) msg = etq + " no puede superar " + ui.num(r.max) + ".";
        valores[r.id] = n;
      } else {
        if (r.tipo === "email" && !RE_EMAIL.test(texto)) msg = etq + " no tiene un formato válido (ej: nombre@correo.com).";
        else if (r.tipo === "telefono" && !RE_TEL.test(texto)) msg = etq + " debe tener entre 7 y 20 dígitos (se permiten + - ( ) y espacios).";
        else if (r.tipo === "documento" && !RE_DOC.test(texto)) msg = etq + " debe tener entre 4 y 20 caracteres alfanuméricos.";
        else if (r.patron && !r.patron.test(texto)) msg = r.mensaje || (etq + " no tiene un formato válido.");
        else if (r.largoMin && texto.length < r.largoMin) msg = etq + " debe tener al menos " + r.largoMin + " caracteres.";
        else if (r.largoMax && texto.length > r.largoMax) msg = etq + " no puede superar " + r.largoMax + " caracteres.";
        valores[r.id] = texto;
      }

      if (msg) {
        errores.push(msg);
        if (el) { el.classList.add("invalido"); el.setAttribute("aria-invalid", "true"); }
      }
    });

    return { ok: errores.length === 0, errores: errores, valores: valores };
  };

  /* Muestra los errores de validación en el aviso y enfoca el primer campo inválido. */
  ui.reportarValidacion = function (resultado) {
    ui.aviso(resultado.errores.join(" "), "error");
    var primero = document.querySelector(".invalido");
    if (primero && primero.focus) primero.focus();
  };

  /* ---------- Exportar CSV real (con BOM para que Excel respete tildes) ---------- */
  ui.descargarCsv = function (nombreArchivo, columnas, filas) {
    function celda(v) {
      var s = v == null ? "" : String(v);
      return /[",\n;]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s;
    }
    var lineas = [columnas.map(function (c) { return celda(c.titulo); }).join(";")];
    filas.forEach(function (f) {
      lineas.push(columnas.map(function (c) { return celda(typeof c.valor === "function" ? c.valor(f) : f[c.valor]); }).join(";"));
    });
    var blob = new Blob(["\uFEFF" + lineas.join("\r\n")], { type: "text/csv;charset=utf-8" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url; a.download = nombreArchivo; document.body.appendChild(a); a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
  };

  /* Bloquea un botón mientras dura una operación asíncrona (evita doble clic = doble POST). */
  ui.conBoton = async function (boton, tarea) {
    if (boton) { if (boton.disabled) return; boton.disabled = true; }
    try { return await tarea(); }
    finally { if (boton) boton.disabled = false; }
  };

  App.ui = ui;
})(window.App = window.App || {});
