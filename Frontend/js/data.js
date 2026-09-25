/* ==========================================================================
   DATA.JS — Capa de datos compartida (El Cuate)
   --------------------------------------------------------------------------
   Simula el backend con localStorage. Mesas e Inventario leen y escriben
   sobre las MISMAS claves, así que agregar un producto a una mesa descuenta
   el stock real del inventario. Cuando exista API/backend, basta con
   reemplazar las funciones de este archivo (los módulos no se tocan).
   ========================================================================== */

const ELCUATE_DB = {
  PRODUCTOS: "elcuate_productos",
  MESAS: "elcuate_mesas",
  VERSION: "elcuate_db_version"
};

const DB_VERSION_ACTUAL = 1;

/* ---------- Catálogo inicial (semilla, tomado de tus capturas) ---------- */
const PRODUCTOS_SEMILLA = [
  { id: "CER-001", nombre: "Águila Original 330 ml", categoria: "Cervezas", costo: 2400, precio: 7500, stock: 45, stockMinimo: 15, proveedor: "Bavaria S.A." },
  { id: "CER-002", nombre: "Águila Light 330 ml", categoria: "Cervezas", costo: 2400, precio: 7500, stock: 38, stockMinimo: 15, proveedor: "Bavaria S.A." },
  { id: "CER-003", nombre: "Poker 330 ml", categoria: "Cervezas", costo: 2300, precio: 7000, stock: 52, stockMinimo: 15, proveedor: "Bavaria S.A." },
  { id: "CER-004", nombre: "Club Colombia Dorada 330 ml", categoria: "Cervezas", costo: 2800, precio: 9500, stock: 34, stockMinimo: 15, proveedor: "Bavaria S.A." },
  { id: "CER-005", nombre: "Corona Extra 330 ml", categoria: "Cervezas", costo: 3200, precio: 11000, stock: 4, stockMinimo: 15, proveedor: "Central Cervecera de Colombia" },
  { id: "CER-006", nombre: "Budweiser 330 ml", categoria: "Cervezas", costo: 2600, precio: 8500, stock: 28, stockMinimo: 15, proveedor: "Diageo Colombia" },
  { id: "CER-007", nombre: "Heineken 330 ml", categoria: "Cervezas", costo: 3000, precio: 10000, stock: 22, stockMinimo: 15, proveedor: "Central Cervecera de Colombia" },
  { id: "CER-008", nombre: "Stella Artois 330 ml", categoria: "Cervezas", costo: 3100, precio: 10500, stock: 16, stockMinimo: 15, proveedor: "Diageo Colombia" },
  { id: "CER-009", nombre: "BBC Chapinero Porter", categoria: "Cervezas", costo: 3800, precio: 12000, stock: 8, stockMinimo: 10, proveedor: "Proveedor Local" },
  { id: "AGU-001", nombre: "Aguardiente Antioqueño Azul 750ml", categoria: "Aguardientes", costo: 28000, precio: 95000, stock: 8, stockMinimo: 10, proveedor: "Dislicores" },
  { id: "AGU-002", nombre: "Aguardiente Antioqueño Verde 750ml", categoria: "Aguardientes", costo: 28000, precio: 95000, stock: 6, stockMinimo: 10, proveedor: "Dislicores" },
  { id: "AGU-003", nombre: "Aguardiente Antioqueño Rojo 750ml", categoria: "Aguardientes", costo: 28000, precio: 95000, stock: 10, stockMinimo: 10, proveedor: "Dislicores" },
  { id: "AGU-004", nombre: "Aguardiente Néctar Azul 750ml", categoria: "Aguardientes", costo: 26000, precio: 88000, stock: 12, stockMinimo: 10, proveedor: "Dislicores" },
  { id: "WHI-001", nombre: "Old Parr 12 años 750ml", categoria: "Whiskies", costo: 85000, precio: 285000, stock: 2, stockMinimo: 5, proveedor: "Diageo Colombia" },
  { id: "WHI-002", nombre: "Old Parr 18 años 750ml", categoria: "Whiskies", costo: 120000, precio: 395000, stock: 3, stockMinimo: 5, proveedor: "Diageo Colombia" },
  { id: "WHI-003", nombre: "Buchanan's Deluxe 750ml", categoria: "Whiskies", costo: 78000, precio: 265000, stock: 5, stockMinimo: 5, proveedor: "Diageo Colombia" },
  { id: "CAF-001", nombre: "Espresso", categoria: "Café", costo: 1800, precio: 5000, stock: 120, stockMinimo: 20, proveedor: "Café Local" },
  { id: "CAF-002", nombre: "Americano", categoria: "Café", costo: 1600, precio: 4000, stock: 120, stockMinimo: 20, proveedor: "Café Local" },
  { id: "CAF-003", nombre: "Cappuccino", categoria: "Café", costo: 2200, precio: 6000, stock: 90, stockMinimo: 20, proveedor: "Café Local" },
  { id: "CAF-004", nombre: "Latte", categoria: "Café", costo: 2200, precio: 6000, stock: 90, stockMinimo: 20, proveedor: "Café Local" },
  { id: "COC-001", nombre: "Croissant", categoria: "Panadería", costo: 3000, precio: 8000, stock: 40, stockMinimo: 10, proveedor: "Panadería Central" },
  { id: "COC-002", nombre: "Sándwich - Pavo", categoria: "Panadería", costo: 6000, precio: 15000, stock: 25, stockMinimo: 10, proveedor: "Panadería Central" }
];

/* ---------- Mesas iniciales (semilla) ---------- */
function mesasSemilla() {
  return [
    { id: 1, numero: 1, personas: 0, abiertoDesde: null, items: [], estadoPago: "ninguno" },
    {
      id: 2, numero: 2, personas: 4, abiertoDesde: "14:32", estadoPago: "ninguno",
      items: [
        { productoId: "CAF-001", cantidad: 2 },
        { productoId: "CAF-003", cantidad: 2 },
        { productoId: "COC-001", cantidad: 1 },
        { productoId: "COC-002", cantidad: 1 },
        { productoId: "CAF-002", cantidad: 1 },
        { productoId: "CAF-004", cantidad: 1 }
      ]
    },
    {
      id: 3, numero: 3, personas: 2, abiertoDesde: "14:15", estadoPago: "ninguno",
      items: [
        { productoId: "CER-004", cantidad: 4 },
        { productoId: "CER-001", cantidad: 4 }
      ]
    },
    {
      id: 4, numero: 4, personas: 3, abiertoDesde: "13:20", estadoPago: "cerrando",
      items: [
        { productoId: "WHI-003", cantidad: 1 },
        { productoId: "CER-007", cantidad: 6 }
      ]
    },
    { id: 5, numero: 5, personas: 0, abiertoDesde: null, items: [], estadoPago: "ninguno" },
    {
      id: 6, numero: 6, personas: 2, abiertoDesde: "14:50", estadoPago: "ninguno",
      items: [
        { productoId: "WHI-001", cantidad: 1 },
        { productoId: "CER-006", cantidad: 8 }
      ]
    }
  ];
}

/* ---------- Inicialización ---------- */
function inicializarDB() {
  const version = localStorage.getItem(ELCUATE_DB.VERSION);
  if (version !== String(DB_VERSION_ACTUAL)) {
    localStorage.setItem(ELCUATE_DB.PRODUCTOS, JSON.stringify(PRODUCTOS_SEMILLA));
    localStorage.setItem(ELCUATE_DB.MESAS, JSON.stringify(mesasSemilla()));
    localStorage.setItem(ELCUATE_DB.VERSION, String(DB_VERSION_ACTUAL));
    return;
  }
  if (!localStorage.getItem(ELCUATE_DB.PRODUCTOS)) {
    localStorage.setItem(ELCUATE_DB.PRODUCTOS, JSON.stringify(PRODUCTOS_SEMILLA));
  }
  if (!localStorage.getItem(ELCUATE_DB.MESAS)) {
    localStorage.setItem(ELCUATE_DB.MESAS, JSON.stringify(mesasSemilla()));
  }
}

/* ---------- Productos ---------- */
function obtenerProductos() {
  return JSON.parse(localStorage.getItem(ELCUATE_DB.PRODUCTOS) || "[]");
}

function guardarProductos(productos) {
  localStorage.setItem(ELCUATE_DB.PRODUCTOS, JSON.stringify(productos));
}

function obtenerProductoPorId(id) {
  return obtenerProductos().find(p => p.id === id) || null;
}

function generarSkuUnico(baseSku) {
  const productos = obtenerProductos();
  let sku = baseSku.trim();
  if (!productos.some(p => p.id === sku)) return sku;
  let n = 2;
  while (productos.some(p => p.id === `${sku}-${n}`)) n++;
  return `${sku}-${n}`;
}

/** Suma/resta stock de un producto. delta negativo = descuenta (venta), positivo = repone. */
function ajustarStock(productoId, delta) {
  const productos = obtenerProductos();
  const idx = productos.findIndex(p => p.id === productoId);
  if (idx === -1) return false;
  const nuevoStock = productos[idx].stock + delta;
  productos[idx].stock = Math.max(0, nuevoStock);
  guardarProductos(productos);
  return true;
}

/* ---------- Mesas ---------- */
function obtenerMesas() {
  return JSON.parse(localStorage.getItem(ELCUATE_DB.MESAS) || "[]");
}

function guardarMesas(mesas) {
  localStorage.setItem(ELCUATE_DB.MESAS, JSON.stringify(mesas));
}

function obtenerMesaPorId(id) {
  return obtenerMesas().find(m => m.id === id) || null;
}

function calcularEstadoMesa(mesa) {
  if (mesa.estadoPago === "cerrando") return "cerrando";
  if (mesa.items && mesa.items.length > 0) return "ocupada";
  return "libre";
}

function calcularTotalesMesa(mesa) {
  const productos = obtenerProductos();
  let subtotal = 0;
  const filas = (mesa.items || []).map(item => {
    const prod = productos.find(p => p.id === item.productoId);
    const precio = prod ? prod.precio : 0;
    const nombre = prod ? prod.nombre : "Producto eliminado";
    const sub = precio * item.cantidad;
    subtotal += sub;
    return { ...item, nombre, precio, subtotal: sub, stockDisponible: prod ? prod.stock : 0 };
  });
  const iva = Math.round(subtotal * 0.19);
  return { filas, subtotal, iva, total: subtotal + iva };
}

/* ---------- Formato de moneda (COP) ---------- */
function formatoCOP(valor) {
  return "$ " + Math.round(valor).toLocaleString("es-CO");
}

function horaActual() {
  const ahora = new Date();
  return ahora.getHours().toString().padStart(2, "0") + ":" + ahora.getMinutes().toString().padStart(2, "0");
}

function fechaHoy() {
  const ahora = new Date();
  return `${ahora.getDate()}/${ahora.getMonth() + 1}/${ahora.getFullYear()}`;
}

inicializarDB();
