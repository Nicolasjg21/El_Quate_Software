/* ==========================================================================
   HISTORIAL-DATA.JS — Capa de datos del módulo Historiales
   --------------------------------------------------------------------------
   Simula el backend con localStorage, en claves propias (no toca las de
   data.js). Genera órdenes pagadas y compras realizadas con fechas RELATIVAS
   al día en que se abre la aplicación por primera vez, para que los filtros
   "Hoy / Esta semana / Este mes / Todos" siempre tengan datos coherentes.
   Cuando exista API real, basta con reemplazar las funciones de este
   archivo (los módulos de UI no se tocan).
   ========================================================================== */

const HIST_DB = {
  ORDENES: "elcuate_hist_ordenes",
  COMPRAS: "elcuate_hist_compras",
  VERSION: "elcuate_hist_version"
};

const HIST_DB_VERSION_ACTUAL = 1;

/* ---------- Utilidades de fecha ---------- */
function histPad(n) { return n.toString().padStart(2, "0"); }

function histFechaISO(fecha) {
  return `${fecha.getFullYear()}-${histPad(fecha.getMonth() + 1)}-${histPad(fecha.getDate())}`;
}

function histFormatoFecha(fecha) {
  return `${histPad(fecha.getDate())}/${histPad(fecha.getMonth() + 1)}/${fecha.getFullYear()}`;
}

function histFormatoFechaHora(fecha) {
  return `${histFormatoFecha(fecha)} ${histPad(fecha.getHours())}:${histPad(fecha.getMinutes())}`;
}

/** Construye una fecha a partir de "hace N días" a una hora/minuto dados. */
function histFechaHace(dias, horas, minutos) {
  const f = new Date();
  f.setDate(f.getDate() - dias);
  f.setHours(horas, minutos, 0, 0);
  return f;
}

/** Lunes 00:00 de la semana que contiene "fecha" (convención colombiana: semana inicia lunes). */
function histInicioSemana(fecha) {
  const f = new Date(fecha);
  const dia = f.getDay(); // 0 = domingo, 1 = lunes ... 6 = sábado
  const diff = (dia === 0 ? 6 : dia - 1); // días desde el lunes
  f.setDate(f.getDate() - diff);
  f.setHours(0, 0, 0, 0);
  return f;
}

function histEsHoy(fecha, ahora) {
  return fecha.getFullYear() === ahora.getFullYear() &&
    fecha.getMonth() === ahora.getMonth() &&
    fecha.getDate() === ahora.getDate();
}

function histEsEstaSemana(fecha, ahora) {
  const inicio = histInicioSemana(ahora);
  const finDia = new Date(ahora); finDia.setHours(23, 59, 59, 999);
  return fecha >= inicio && fecha <= finDia;
}

function histEsEsteMes(fecha, ahora) {
  return fecha.getFullYear() === ahora.getFullYear() && fecha.getMonth() === ahora.getMonth();
}

/* ---------- Normalización de texto para búsqueda (minúsculas + sin tildes) ---------- */
function histNormalizar(texto) {
  return (texto === undefined || texto === null ? "" : String(texto))
    .toString()
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}

/* ---------- Catálogos ---------- */

/** Productos que se consumen en el bar (para las órdenes pagadas). */
const HIST_CATALOGO_BAR = [
  { nombre: "Águila Original 330 ml", precio: 7500 },
  { nombre: "Poker 330 ml", precio: 7000 },
  { nombre: "Club Colombia Dorada 330 ml", precio: 9500 },
  { nombre: "Corona Extra 330 ml", precio: 11000 },
  { nombre: "Heineken 330 ml", precio: 10000 },
  { nombre: "Aguardiente Antioqueño (copa)", precio: 6500 },
  { nombre: "Ron Viejo de Caldas (copa)", precio: 7500 },
  { nombre: "Ron Medellín Añejo (copa)", precio: 8000 },
  { nombre: "Old Parr 12 Años (copa)", precio: 18000 },
  { nombre: "Buchanan's Deluxe (copa)", precio: 19000 },
  { nombre: "Jack Daniel's (copa)", precio: 16000 },
  { nombre: "Jose Cuervo Especial (copa)", precio: 15000 },
  { nombre: "Red Bull", precio: 9000 },
  { nombre: "Hatsu Energizante", precio: 7500 },
  { nombre: "Coca-Cola", precio: 4500 },
  { nombre: "Coca-Cola Zero", precio: 4500 },
  { nombre: "Agua con Gas", precio: 4000 },
  { nombre: "Maní Salado", precio: 7000 },
  { nombre: "Nachos con Queso", precio: 18000 },
  { nombre: "Chicharrón Crocante", precio: 22000 },
  { nombre: "Papas Rizadas", precio: 14000 }
];

const HIST_MESEROS = ["Carlos Ramírez", "Laura Rodríguez", "Santiago Gómez", "Valentina Giraldo", "Andrés Cardona", "Juan Camilo Restrepo"];

const HIST_METODOS_PAGO = ["efectivo", "tarjeta", "billetera"];

/** Catálogo de compras a proveedores, agrupado por categoría del negocio. */
const HIST_CATALOGO_COMPRAS = [
  { nombre: "Águila Original 330 ml", categoria: "Cervezas", costo: 2400 },
  { nombre: "Poker 330 ml", categoria: "Cervezas", costo: 2300 },
  { nombre: "Club Colombia Dorada 330 ml", categoria: "Cervezas", costo: 2800 },
  { nombre: "Corona Extra 330 ml", categoria: "Cervezas", costo: 3200 },
  { nombre: "Heineken 330 ml", categoria: "Cervezas", costo: 3000 },
  { nombre: "Budweiser 330 ml", categoria: "Cervezas", costo: 2600 },
  { nombre: "BBC Monserrate Roja 330 ml", categoria: "Cervezas", costo: 3800 },
  { nombre: "Aguardiente Antioqueño 750 ml", categoria: "Licores", costo: 28000 },
  { nombre: "Ron Viejo de Caldas 750 ml", categoria: "Licores", costo: 26000 },
  { nombre: "Ron Medellín Añejo 750 ml", categoria: "Licores", costo: 32000 },
  { nombre: "Old Parr 12 Años 750 ml", categoria: "Licores", costo: 85000 },
  { nombre: "Buchanan's Deluxe 750 ml", categoria: "Licores", costo: 78000 },
  { nombre: "Jack Daniel's 750 ml", categoria: "Licores", costo: 95000 },
  { nombre: "Jose Cuervo Especial 750 ml", categoria: "Licores", costo: 72000 },
  { nombre: "Red Bull", categoria: "Bebidas", costo: 4200 },
  { nombre: "Hatsu Energizante", categoria: "Bebidas", costo: 3600 },
  { nombre: "Coca-Cola", categoria: "Bebidas", costo: 2100 },
  { nombre: "Coca-Cola Zero", categoria: "Bebidas", costo: 2100 },
  { nombre: "Agua con Gas", categoria: "Bebidas", costo: 1800 },
  { nombre: "Néctar de Durazno", categoria: "Bebidas", costo: 2200 },
  { nombre: "Maní Salado", categoria: "Snacks", costo: 3200 },
  { nombre: "Nachos", categoria: "Snacks", costo: 6500 },
  { nombre: "Chicharrón Crocante", categoria: "Snacks", costo: 9000 },
  { nombre: "Papas Rizadas", categoria: "Snacks", costo: 5500 },
  { nombre: "Hielo en Bolsa 5 kg", categoria: "Insumos", costo: 4500 },
  { nombre: "Vasos Desechables x50", categoria: "Insumos", costo: 8500 },
  { nombre: "Servilletas Paquete", categoria: "Insumos", costo: 3000 },
  { nombre: "Pitillos Paquete", categoria: "Insumos", costo: 2500 }
];

const HIST_PROVEEDORES = [
  "Central Cervecera de Colombia",
  "Distribuidora Bavaria",
  "Licores Premium SAS",
  "Distribuciones El Barril",
  "Comercializadora Andina",
  "Licores del Valle",
  "Importadora Premium Spirits",
  "Bodega Central"
];

const HIST_USUARIOS_REGISTRO = ["María López", "Carlos Mendoza", "David García", "Daniela Vélez"];

/* ---------- Generador determinístico (sin dependencias externas) ---------- */
function histCrearGeneradorAleatorio(semilla) {
  let estado = semilla;
  return function () {
    estado = (estado * 1103515245 + 12345) & 0x7fffffff;
    return estado / 0x7fffffff;
  };
}

function histElegir(lista, azar) {
  return lista[Math.floor(azar() * lista.length)];
}

function histEntero(min, max, azar) {
  return Math.floor(azar() * (max - min + 1)) + min;
}

/* ---------- Semilla: Órdenes Pagadas ---------- */
function histGenerarOrdenesSemilla() {
  const azar = histCrearGeneradorAleatorio(20260923);
  // días de antigüedad y cuántas órdenes se generan ese día
  const puntos = [
    { dias: 0, cantidad: 5 },
    { dias: 1, cantidad: 4 },
    { dias: 2, cantidad: 3 },
    { dias: 3, cantidad: 3 },
    { dias: 5, cantidad: 2 },
    { dias: 6, cantidad: 2 },
    { dias: 8, cantidad: 2 },
    { dias: 12, cantidad: 2 },
    { dias: 18, cantidad: 2 },
    { dias: 22, cantidad: 2 },
    { dias: 35, cantidad: 2 },
    { dias: 50, cantidad: 2 },
    { dias: 75, cantidad: 2 }
  ];

  const ordenes = [];
  let consecutivo = 5900;

  puntos.forEach(punto => {
    for (let i = 0; i < punto.cantidad; i++) {
      const horas = histEntero(12, 23, azar);
      const minutos = histEntero(0, 59, azar);
      const fecha = histFechaHace(punto.dias, horas, minutos);

      const cantidadItems = histEntero(2, 4, azar);
      const items = [];
      const usados = new Set();
      for (let j = 0; j < cantidadItems; j++) {
        let producto;
        let intentos = 0;
        do {
          producto = histElegir(HIST_CATALOGO_BAR, azar);
          intentos++;
        } while (usados.has(producto.nombre) && intentos < 10);
        usados.add(producto.nombre);
        items.push({
          nombre: producto.nombre,
          cantidad: histEntero(1, 3, azar),
          precioUnitario: producto.precio
        });
      }

      const subtotal = items.reduce((acc, it) => acc + it.cantidad * it.precioUnitario, 0);
      const impuesto = Math.round(subtotal * 0.08); // INC de bares y restaurantes
      const propina = Math.round(subtotal * 0.10);  // propina voluntaria sugerida
      const total = subtotal + impuesto + propina;

      ordenes.push({
        id: `ORD-2026-${consecutivo}`,
        fechaHora: fecha.toISOString(),
        mesa: histEntero(1, 10, azar),
        mesero: histElegir(HIST_MESEROS, azar),
        estado: "Pagada",
        metodoPago: histElegir(HIST_METODOS_PAGO, azar),
        items,
        subtotal,
        impuesto,
        propina,
        total
      });

      consecutivo--;
    }
  });

  // más recientes primero
  ordenes.sort((a, b) => new Date(b.fechaHora) - new Date(a.fechaHora));
  return ordenes;
}

/* ---------- Semilla: Compras Realizadas ---------- */
function histGenerarComprasSemilla() {
  const azar = histCrearGeneradorAleatorio(19860412);
  const diasAntiguedad = [0, 2, 4, 7, 9, 13, 17, 21, 26, 30, 38, 46, 55, 68, 82, 95, 110, 130];

  const compras = [];
  let consecutivo = 1900;

  diasAntiguedad.forEach(dias => {
    const fecha = histFechaHace(dias, 0, 0);
    const cantidadItems = histEntero(4, 8, azar);
    const items = [];
    const usados = new Set();

    for (let j = 0; j < cantidadItems; j++) {
      let producto;
      let intentos = 0;
      do {
        producto = histElegir(HIST_CATALOGO_COMPRAS, azar);
        intentos++;
      } while (usados.has(producto.nombre) && intentos < 12);
      usados.add(producto.nombre);

      const cantidad = producto.categoria === "Licores" ? histEntero(4, 12, azar) : histEntero(12, 70, azar);
      items.push({
        nombre: producto.nombre,
        categoria: producto.categoria,
        cantidad,
        costoUnitario: producto.costo
      });
    }

    const costoTotal = items.reduce((acc, it) => acc + it.cantidad * it.costoUnitario, 0);

    compras.push({
      id: `CMP-2026-${consecutivo}`,
      fecha: histFechaISO(fecha),
      proveedor: histElegir(HIST_PROVEEDORES, azar),
      factura: `FAC-${1900 + consecutivo - 1900 + histEntero(100, 899, azar)}`,
      registradoPor: histElegir(HIST_USUARIOS_REGISTRO, azar),
      items,
      costoTotal,
      observaciones: histElegir([
        "Reabastecimiento regular de bebidas y suministros.",
        "Compra programada para el fin de semana.",
        "Reposición de licores premium.",
        "Pedido de insumos de barra.",
        ""
      ], azar)
    });

    consecutivo--;
  });

  compras.sort((a, b) => new Date(b.fecha) - new Date(a.fecha));
  return compras;
}

/* ---------- Inicialización ---------- */
function histInicializarDB() {
  const version = localStorage.getItem(HIST_DB.VERSION);
  if (version !== String(HIST_DB_VERSION_ACTUAL) || !localStorage.getItem(HIST_DB.ORDENES)) {
    localStorage.setItem(HIST_DB.ORDENES, JSON.stringify(histGenerarOrdenesSemilla()));
  }
  if (version !== String(HIST_DB_VERSION_ACTUAL) || !localStorage.getItem(HIST_DB.COMPRAS)) {
    localStorage.setItem(HIST_DB.COMPRAS, JSON.stringify(histGenerarComprasSemilla()));
  }
  localStorage.setItem(HIST_DB.VERSION, String(HIST_DB_VERSION_ACTUAL));
}

/* ---------- Acceso: Órdenes ---------- */
function histObtenerOrdenes() {
  return JSON.parse(localStorage.getItem(HIST_DB.ORDENES) || "[]");
}

function histGuardarOrdenes(ordenes) {
  localStorage.setItem(HIST_DB.ORDENES, JSON.stringify(ordenes));
}

/* ---------- Acceso: Compras ---------- */
function histObtenerCompras() {
  return JSON.parse(localStorage.getItem(HIST_DB.COMPRAS) || "[]");
}

function histGuardarCompras(compras) {
  localStorage.setItem(HIST_DB.COMPRAS, JSON.stringify(compras));
}

function histSiguienteIdCompra() {
  const compras = histObtenerCompras();
  const numeros = compras
    .map(c => parseInt(String(c.id).split("-").pop(), 10))
    .filter(n => !isNaN(n));
  const maximo = numeros.length ? Math.max(...numeros) : 1900;
  return `CMP-2026-${maximo + 1}`;
}

histInicializarDB();
