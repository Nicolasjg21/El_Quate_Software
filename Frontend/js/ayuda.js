/**
 * @file ayuda.js
 * @description Página de ayuda y soporte (RF15).
 *   - Muestra las preguntas frecuentes agrupadas por módulo; cada respuesta se abre y se cierra al pulsar la pregunta.
 *   - Filtra las preguntas por palabra clave, sin distinguir tildes ni mayúsculas.
 *   - Muestra solo las secciones de los módulos a los que el rol tiene permiso. Sin sesión (abierta desde el
 *     inicio de sesión) muestra únicamente las preguntas de ingreso.
 *   - Toma los datos de contacto de ELCUATE_CONFIG.SOPORTE y la versión de ELCUATE_CONFIG.VERSION.
 *   - Abre directamente la sección indicada en la URL (#mesas, #inventario…) cuando se llega desde el botón "?".
 *   - "Reportar problema" abre el correo con la pantalla de origen, el usuario, la versión y la hora.
 *   - Muestra el enlace al manual de usuario solo si el archivo existe.
 *   El contenido es estático: no consulta la API. Filtrar por permiso aquí es una ayuda de navegación,
 *   no un control de seguridad; cada acción la sigue validando el Backend.
 * @requires config.js, auth.js
 */

(function () {
  "use strict";

  /**
   * Secciones de preguntas frecuentes.
   * @typedef {{p: string, r: string}} Pregunta
   * @typedef {{id: string, titulo: string, icono: string, destacada: number, publica?: boolean, permiso?: string, preguntas: Pregunta[]}} Seccion
   *   icono:     contenido SVG (24×24, trazo) de la tarjeta del tema; mismo estilo de íconos del menú lateral.
   *   destacada: índice de la pregunta que aparece en "temas frecuentes" bajo el buscador.
   *   publica:   se muestra también sin sesión.
   *   permiso:   nombre del permiso requerido (PermisosSistema del Backend). Sin permiso: cualquier usuario con sesión.
   * @type {Seccion[]}
   */
  const SECCIONES = [
    {
      id: "ingreso", titulo: "Ingreso y sesión", publica: true, destacada: 2,
      icono: '<rect width="18" height="11" x="3" y="11" rx="2" /><path d="M7 11V7a5 5 0 0 1 10 0v4" />',
      preguntas: [
        { p: "¿Cómo ingreso al sistema?",
          r: "En Iniciar sesión escriba su correo y su contraseña y pulse Acceder. Según su rol, el sistema abre el Panel Principal o la pantalla de Mesas." },
        { p: "El sistema dice que los datos son incorrectos, ¿qué reviso?",
          r: "Por seguridad, el mensaje es el mismo si el correo no existe o si la contraseña no coincide. Revise que el correo esté bien escrito y que la contraseña respete mayúsculas y minúsculas." },
        { p: "Mi acceso quedó bloqueado, ¿qué hago?",
          r: "Después de 5 intentos fallidos en 15 minutos, el acceso se bloquea temporalmente aunque luego escriba la contraseña correcta. Espere unos 15 minutos e intente de nuevo, o comuníquese con soporte." },
        { p: "Olvidé mi contraseña.",
          r: "El sistema no envía correos de recuperación. Comuníquese con el administrador (sección Soporte) para que le ayude a recuperar el acceso." },
        { p: "¿Por qué el primer ingreso del día tarda más?",
          r: "Cuando el sistema lleva un tiempo sin uso, la base de datos tarda en reanudarse. Aparece el aviso Conectando con el servidor y puede tardar hasta un minuto. Las siguientes operaciones responden con normalidad." },
        { p: "¿Por qué se cerró mi sesión?",
          r: "La sesión se cierra al cerrar la pestaña del navegador, cuando vence su tiempo o cuando se cambia la contraseña. Inicie sesión nuevamente." }
      ]
    },
    {
      id: "cuenta", titulo: "Mi cuenta", destacada: 0,
      icono: '<circle cx="12" cy="8" r="5" /><path d="M20 21a8 8 0 0 0-16 0" />',
      preguntas: [
        { p: "¿Cómo cambio mi contraseña?",
          r: "Vaya a Configuración, sección Cambiar contraseña. Escriba la contraseña actual y la nueva (entre 8 y 128 caracteres) y pulse Guardar contraseña. Las sesiones abiertas con la contraseña anterior dejan de servir." },
        { p: "¿Dónde veo mis datos?",
          r: "En Configuración, sección Mi información." }
      ]
    },
    {
      id: "mesas", titulo: "Mesas y pedidos", destacada: 1,
      icono: '<path d="M3 3h18v4H3z" /><path d="M5 7v14" /><path d="M19 7v14" />',
      preguntas: [
        { p: "¿Qué significa el estado de cada mesa?",
          r: "Libre: sin cuenta abierta. Ocupada: tiene una cuenta abierta. Cerrando: el cliente pidió la cuenta y falta el cobro. Las mesas retiradas del salón se muestran aparte, solo para el administrador." },
        { p: "¿Cómo tomo un pedido?",
          r: "En Mesas, seleccione la mesa y pulse + Agregar Productos. Al agregar el primer producto a una mesa libre se abre su cuenta. Cada mesa tiene una sola cuenta abierta a la vez." },
        { p: "¿Puedo cambiar la cantidad o quitar un producto?",
          r: "Sí, mientras la cuenta esté abierta. Al quitar un producto, su cantidad vuelve al inventario." },
        { p: "¿Por qué no puedo agregar un producto?",
          r: "El producto está agotado o fue desactivado en el inventario. Avise al encargado del inventario." },
        { p: "¿Cómo pido la cuenta de una mesa?",
          r: "En la mesa, pulse Confirmar Pago. La mesa pasa a Cerrando y queda lista para que Caja registre el cobro. Si el cliente pide algo más, use Reabrir cuenta." }
      ]
    },
    {
      id: "cobros", titulo: "Cobros y comprobantes", permiso: "cobros.gestionar", destacada: 0,
      icono: '<rect width="20" height="14" x="2" y="5" rx="2" /><path d="M2 10h20" />',
      preguntas: [
        { p: "¿Cómo cobro una mesa?",
          r: "En la mesa en estado Cerrando, pulse Marcar como Pagada y Liberar Mesa, elija el método de pago y pulse Registrar pago y liberar mesa. Se muestra el comprobante, la cuenta queda cerrada y la mesa libre." },
        { p: "No aparece ningún método de pago al cobrar.",
          r: "Si todavía no hay métodos registrados, el formulario de cobro permite crear el primero con Agregar método (por ejemplo Efectivo, Nequi o Daviplata)." },
        { p: "¿Cómo vuelvo a imprimir un comprobante?",
          r: "En Historiales, pestaña Órdenes Pagadas, busque la orden y ábrala para ver e imprimir su comprobante." },
        { p: "¿El comprobante es una factura electrónica?",
          r: "No. El comprobante es un documento interno del bar con el detalle, el IVA (19 %), el total y el método de pago." }
      ]
    },
    {
      id: "inventario", titulo: "Inventario, proveedores y compras", permiso: "inventario.gestionar", destacada: 2,
      icono: '<path d="m7.5 4.27 9 5.15" /><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z" /><path d="m3.3 7 8.7 5 8.7-5" /><path d="M12 22V12" />',
      preguntas: [
        { p: "¿Cómo creo un producto?",
          r: "En Inventario, pulse + Nuevo Producto. El precio se escribe en pesos, sin centavos." },
        { p: "¿Por qué no puedo eliminar un producto?",
          r: "Un producto con movimientos de inventario no se elimina, para conservar el historial. Desactívelo: deja de ofrecerse al tomar pedidos." },
        { p: "¿Qué significan stock bajo y agotado?",
          r: "Stock bajo: las existencias llegaron a la cantidad mínima del producto o menos. Agotado: no quedan existencias y el producto no se puede agregar a un pedido. La alerta desaparece al registrar una compra." },
        { p: "¿Cómo registro una compra?",
          r: "En Historiales, pestaña Compras Realizadas, pulse + Registrar Nueva Compra, elija el proveedor y agregue los productos con su cantidad y costo. El stock aumenta al guardar." },
        { p: "¿Por qué no puedo eliminar un proveedor?",
          r: "Un proveedor con compras registradas no se elimina, para conservar el historial de compras." }
      ]
    },
    {
      id: "reportes", titulo: "Panel, ventas y auditoría", permiso: "auditorias.consultar", destacada: 1,
      icono: '<path d="M3 3v18h18" /><path d="m19 9-5 5-4-4-3 3" />',
      preguntas: [
        { p: "¿Qué muestra el Panel Principal?",
          r: "La tendencia de ventas y órdenes, los ingresos por categoría, los productos más vendidos y el estado del inventario con las alertas de stock bajo y agotado." },
        { p: "¿Cómo consulto las ventas de un periodo?",
          r: "En Ventas y Analíticas, elija el rango de fechas. Pulse Exportar CSV para descargar lo que ve en pantalla." },
        { p: "¿Qué registra la auditoría?",
          r: "Quién hizo cada operación importante, cuándo y sobre qué registro, con los datos antes y después del cambio. Use los filtros para buscar por módulo, acción, usuario o rango de fechas." }
      ]
    },
    {
      id: "usuarios", titulo: "Usuarios y roles", permiso: "usuarios.gestionar", destacada: 0,
      icono: '<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />',
      preguntas: [
        { p: "¿Cómo creo un usuario?",
          r: "En Usuarios, pulse + Nuevo usuario, complete los datos, asigne un rol y una contraseña inicial. El correo no puede repetirse." },
        { p: "¿Cómo impido que alguien ingrese?",
          r: "Edite el usuario y cambie su estado a Inactivo. Un usuario inactivo no puede iniciar sesión." },
        { p: "¿Cómo cambio lo que puede hacer un rol?",
          r: "En Usuarios, pulse Roles y permisos, elija el rol y marque o desmarque sus permisos. Los cambios aplican a todos los usuarios con ese rol." },
        { p: "¿Por qué no puedo eliminar un rol?",
          r: "Un rol asignado a algún usuario no se puede eliminar. Primero asigne otro rol a esos usuarios." }
      ]
    }
  ];

  const $ = (id) => document.getElementById(id);

  /**
   * Quita tildes y pasa a minúsculas para comparar sin distinguirlas.
   * @param {string} texto
   * @returns {string}
   */
  function normalizar(texto) {
    return String(texto || "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();
  }

  /**
   * Secciones visibles para el usuario actual.
   * @param {boolean} conSesion
   * @returns {Seccion[]}
   */
  function seccionesVisibles(conSesion) {
    if (!conSesion) return SECCIONES.filter((s) => s.publica);
    return SECCIONES.filter((s) => !s.permiso || window.Auth.tiene(s.permiso));
  }

  /**
   * Crea el bloque de una pregunta con su respuesta oculta (details/summary nativo: accesible con teclado).
   * @param {Pregunta} pregunta
   * @returns {HTMLDetailsElement}
   */
  function crearPregunta(pregunta) {
    const det = document.createElement("details");
    det.className = "ayuda-pregunta";
    det.dataset.texto = normalizar(pregunta.p + " " + pregunta.r);
    const sum = document.createElement("summary");
    sum.textContent = pregunta.p;
    sum.dataset.original = pregunta.p;
    const resp = document.createElement("p");
    resp.className = "ayuda-respuesta";
    resp.textContent = pregunta.r;
    resp.dataset.original = pregunta.r;
    det.append(sum, resp);
    return det;
  }

  /**
   * Pinta las secciones y el mensaje para cuando la búsqueda no tiene resultados.
   * @param {Seccion[]} secciones
   */
  function pintar(secciones) {
    const cont = $("ayuda-secciones");
    cont.textContent = "";
    secciones.forEach((s) => {
      const bloque = document.createElement("section");
      bloque.className = "panel ayuda-seccion";
      bloque.dataset.seccion = s.id;
      bloque.id = "seccion-" + s.id;
      const h = document.createElement("h3");
      h.className = "ayuda-seccion-titulo";
      h.textContent = s.titulo;
      bloque.appendChild(h);
      s.preguntas.forEach((p, i) => {
        const det = crearPregunta(p);
        det.id = "pregunta-" + s.id + "-" + i;
        bloque.appendChild(det);
      });
      cont.appendChild(bloque);
    });
    pintarDestacados(secciones);
    pintarTemas(secciones);
    const vacio = document.createElement("p");
    vacio.className = "panel ayuda-vacio";
    vacio.id = "ayuda-vacio";
    vacio.hidden = true;
    vacio.textContent = "No se encontraron preguntas con esa palabra. Pruebe con otra o comuníquese con soporte.";
    cont.appendChild(vacio);
  }

  /**
   * Crea un ícono SVG con el mismo estilo de trazo que los íconos del menú lateral.
   * @param {string} contenido Elementos SVG internos.
   * @returns {HTMLSpanElement}
   */
  function crearIcono(contenido) {
    const span = document.createElement("span");
    span.className = "ayuda-tema-icono";
    span.setAttribute("aria-hidden", "true");
    // Contenido fijo definido en SECCIONES (no proviene del usuario ni de la API).
    span.innerHTML = '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' + contenido + "</svg>";
    return span;
  }

  /**
   * Abre una pregunta y la lleva a la vista, limpiando antes cualquier búsqueda.
   * @param {string} idPregunta
   */
  function abrirPregunta(idPregunta) {
    const buscar = $("ayuda-buscar");
    if (buscar.value) { buscar.value = ""; filtrar(""); }
    const det = document.getElementById(idPregunta);
    if (!det) return;
    det.open = true;
    det.scrollIntoView({ block: "center", behavior: "smooth" });
    det.querySelector("summary").focus({ preventScroll: true });
  }

  /**
   * Bajo el buscador: hasta 4 preguntas frecuentes, una por tema visible (como "temas populares").
   * @param {Seccion[]} secciones
   */
  function pintarDestacados(secciones) {
    const cont = $("ayuda-destacados");
    cont.textContent = "";
    secciones.slice(0, 4).forEach((s) => {
      const i = Math.min(s.destacada || 0, s.preguntas.length - 1);
      const a = document.createElement("a");
      a.className = "ayuda-destacado";
      a.href = "#pregunta-" + s.id + "-" + i;
      const cat = document.createElement("span");
      cat.className = "ayuda-destacado-categoria";
      cat.textContent = s.titulo;
      const txt = document.createElement("span");
      txt.className = "ayuda-destacado-texto";
      txt.textContent = s.preguntas[i].p;
      a.append(cat, txt);
      a.addEventListener("click", (e) => { e.preventDefault(); abrirPregunta("pregunta-" + s.id + "-" + i); });
      cont.appendChild(a);
    });
  }

  /**
   * Tarjetas de temas: una por sección visible, con ícono y número de preguntas.
   * @param {Seccion[]} secciones
   */
  function pintarTemas(secciones) {
    const cont = $("ayuda-temas");
    cont.textContent = "";
    secciones.forEach((s) => {
      const a = document.createElement("a");
      a.className = "ayuda-tema";
      a.href = "#seccion-" + s.id;
      const titulo = document.createElement("span");
      titulo.className = "ayuda-tema-titulo";
      titulo.textContent = s.titulo;
      const n = document.createElement("span");
      n.className = "ayuda-tema-cantidad";
      n.textContent = s.preguntas.length === 1 ? "1 pregunta" : s.preguntas.length + " preguntas";
      a.append(crearIcono(s.icono), titulo, n);
      a.addEventListener("click", (e) => {
        e.preventDefault();
        const sec = document.getElementById("seccion-" + s.id);
        document.querySelectorAll(".ayuda-seccion--actual").forEach((x) => x.classList.remove("ayuda-seccion--actual"));
        sec.classList.add("ayuda-seccion--actual");
        sec.scrollIntoView({ block: "start", behavior: "smooth" });
      });
      cont.appendChild(a);
    });
  }

  /**
   * Escribe el texto de un elemento con las coincidencias de la búsqueda en negrilla.
   * La comparación ignora tildes y mayúsculas ("contrasena" resalta "contraseña"), por eso se arma
   * un mapa de cada carácter normalizado a su posición en el texto original.
   * Se construye con nodos de texto (sin innerHTML), así que el texto nunca se interpreta como HTML.
   * @param {HTMLElement} el Elemento con data-original.
   * @param {string} q Búsqueda ya normalizada; vacía quita el resaltado.
   */
  function resaltar(el, q) {
    const original = el.dataset.original || "";
    if (!q) { el.textContent = original; return; }

    let normal = "";
    const posicion = [];
    for (let i = 0; i < original.length; i++) {
      const n = normalizar(original[i]) || (original[i] === " " ? " " : "");
      for (const c of n) { normal += c; posicion.push(i); }
    }

    const frag = document.createDocumentFragment();
    let desde = 0;
    let idx = normal.indexOf(q);
    while (idx !== -1) {
      const ini = posicion[idx];
      const fin = posicion[idx + q.length - 1] + 1;
      if (ini > desde) frag.appendChild(document.createTextNode(original.slice(desde, ini)));
      const b = document.createElement("strong");
      b.className = "ayuda-resaltado";
      b.textContent = original.slice(ini, fin);
      frag.appendChild(b);
      desde = fin;
      idx = normal.indexOf(q, idx + q.length);
    }
    if (desde < original.length) frag.appendChild(document.createTextNode(original.slice(desde)));
    el.textContent = "";
    el.appendChild(frag);
  }

  /**
   * Muestra solo las preguntas que contienen el texto buscado y actualiza el conteo.
   * Con la búsqueda vacía se ven todas las preguntas, cerradas.
   * @param {string} texto
   */
  function filtrar(texto) {
    const q = normalizar(texto);
    let total = 0;
    document.querySelectorAll(".ayuda-seccion").forEach((sec) => {
      let visibles = 0;
      sec.querySelectorAll(".ayuda-pregunta").forEach((det) => {
        const coincide = !q || det.dataset.texto.indexOf(q) !== -1;
        det.hidden = !coincide;
        det.open = !!q && coincide;
        det.querySelectorAll("[data-original]").forEach((el) => resaltar(el, coincide ? q : ""));
        if (coincide) visibles++;
      });
      sec.hidden = visibles === 0;
      total += visibles;
    });
    $("ayuda-vacio").hidden = total > 0;
    $("ayuda-bloque-temas").hidden = !!q;
    $("ayuda-conteo").textContent = q
      ? (total === 1 ? "1 pregunta encontrada" : total + " preguntas encontradas")
      : "";
  }

  /** Nombre legible de cada pantalla, para el reporte de problemas. */
  const NOMBRE_PANTALLA = {
    "dashboard.html": "Panel Principal", "analiticas.html": "Ventas y Analíticas", "auditoria.html": "Auditoría",
    "mesas.html": "Mesas", "inventario.html": "Inventario", "proveedores.html": "Proveedores",
    "historiales.html": "Historiales", "usuarios.html": "Usuarios", "configuracion.html": "Configuración",
    "login.html": "Iniciar sesión"
  };

  /** Pantalla desde la que se abrió la ayuda (parámetro ?desde=), o "Ayuda" si se abrió directamente. */
  function pantallaOrigen() {
    const desde = new URLSearchParams(location.search).get("desde") || "";
    return NOMBRE_PANTALLA[desde.toLowerCase()] || "Ayuda";
  }

  /**
   * Arma el enlace mailto del reporte de problema.
   * @param {string} correo Correo de soporte.
   * @returns {string}
   */
  function enlaceReporte(correo) {
    const cfg = window.ELCUATE_CONFIG || {};
    const u = window.Auth.sesionValida() ? window.Auth.usuario() : null;
    const pantalla = pantallaOrigen();
    const asunto = "[El Cuate " + (cfg.VERSION || "") + "] Problema en " + pantalla;
    const cuerpo = [
      "Pantalla: " + pantalla,
      "Usuario: " + (u && u.email ? u.email : "sin sesión"),
      "Versión: " + (cfg.VERSION || "—"),
      "Fecha y hora: " + new Date().toLocaleString("es-CO", { timeZone: "America/Bogota" }),
      "Navegador: " + navigator.userAgent,
      "",
      "¿Qué estaba haciendo?",
      "",
      "¿Qué mensaje apareció?",
      ""
    ].join("\n");
    return "mailto:" + correo + "?subject=" + encodeURIComponent(asunto) + "&body=" + encodeURIComponent(cuerpo);
  }

  /** Muestra el enlace al manual solo si el PDF existe en el servidor. */
  async function prepararManual() {
    const url = window.ELCUATE_CONFIG && window.ELCUATE_CONFIG.MANUAL_URL;
    if (!url) return;
    try {
      const r = await fetch(url, { method: "HEAD", cache: "no-store" });
      if (r.ok) {
        const a = $("ayuda-manual");
        a.href = url;
        a.hidden = false;
        $("ayuda-manual-pendiente").hidden = true;
      }
    } catch (e) { /* sin manual publicado: el enlace queda oculto */ }
  }

  /** Abre y resalta la sección indicada en la URL (#mesas, #inventario…), si el rol puede verla. */
  function irASeccion() {
    const id = decodeURIComponent((location.hash || "").slice(1));
    if (!id) return;
    const sec = document.querySelector('.ayuda-seccion[data-seccion="' + id.replace(/[^a-z]/g, "") + '"]');
    if (!sec) return;
    sec.classList.add("ayuda-seccion--actual");
    sec.scrollIntoView({ block: "start" });
  }

  /** Llena la sección Soporte con ELCUATE_CONFIG.SOPORTE y prepara el reporte de problemas. */
  function pintarSoporte() {
    const cfg = window.ELCUATE_CONFIG || {};
    $("soporte-version").textContent = cfg.VERSION || "—";
    const s = cfg.SOPORTE || {};
    const reportar = $("ayuda-reportar");
    if (s.correo) {
      // El enlace se arma al cargar y se vuelve a armar al pulsar, para que la hora sea la del reporte.
      reportar.href = enlaceReporte(s.correo);
      reportar.addEventListener("click", () => { reportar.href = enlaceReporte(s.correo); });
    } else {
      reportar.classList.add("btn--deshabilitado");
      reportar.setAttribute("aria-disabled", "true");
      reportar.removeAttribute("href");
      $("ayuda-reportar-aviso").hidden = false;
    }
    $("soporte-responsable").textContent = s.responsable || "—";
    $("soporte-telefono").textContent = s.telefono || "—";
    $("soporte-horario").textContent = s.horario || "—";
    const correo = $("soporte-correo");
    correo.textContent = s.correo || "—";
    if (s.correo) correo.href = "mailto:" + s.correo + "?subject=" + encodeURIComponent("Soporte El Cuate");
    else correo.removeAttribute("href");
  }

  /** Sin sesión: oculta el menú y el usuario del encabezado y ofrece volver al inicio de sesión. */
  function modoSinSesion() {
    document.body.classList.add("ayuda-sin-sesion");
    $("ayuda-volver-login").hidden = false;
  }

  document.addEventListener("DOMContentLoaded", async () => {
    pintarSoporte();
    const conSesion = window.Auth.sesionValida();
    if (!conSesion) modoSinSesion();
    else await window.Auth.listo;
    pintar(seccionesVisibles(conSesion));
    irASeccion();
    prepararManual();
    let espera;
    $("ayuda-buscar").addEventListener("input", (e) => {
      clearTimeout(espera);
      espera = setTimeout(() => filtrar(e.target.value), 150);
    });
  });
})();
