/**
 * @file auditoria-data.js
 * @description Datos del módulo Auditoría: consulta de registros y presentación en lenguaje natural.
 *   La base almacena códigos técnicos (INSERT, UPDATE, DELETE, LOGIN, LOGOUT) y nombres de tabla;
 *   este módulo los presenta como verbos en pasado (Creó, Modificó, Eliminó, Inició sesión, Cerró sesión),
 *   módulos y campos legibles.
 * @requires api-cliente.js, data.js
 *
 * Endpoints: api/Auditorias/GetAuditorias (requiere "auditorias.consultar"),
 *            api/Usuarios/GetUsuarios (opcional, para mostrar nombres en lugar de ids).
 */

/** Códigos de acción almacenados en Auditorias.accion. */
const AUDITORIA_ACCIONES = ["INSERT", "UPDATE", "DELETE", "LOGIN", "LOGOUT"];

/** Código de acción → verbo mostrado al usuario. */
const AUDITORIA_VERBOS = {
  INSERT: "Creó",
  UPDATE: "Modificó",
  DELETE: "Eliminó",
  LOGIN: "Inició sesión",
  LOGOUT: "Cerró sesión"
};

/** Tabla → nombre del módulo y forma de referirse a uno de sus registros. */
const AUDITORIA_MODULOS = {
  Usuarios:      { modulo: "Usuarios",               objeto: "el usuario" },
  Proveedores:   { modulo: "Proveedores",            objeto: "el proveedor" },
  Productos:     { modulo: "Productos",              objeto: "el producto" },
  Categorias:    { modulo: "Categorías",             objeto: "la categoría" },
  MetodosPago:   { modulo: "Métodos de pago",        objeto: "el método de pago" },
  Inventario:    { modulo: "Inventario (stock)",     objeto: "el stock de" },
  Kardex:        { modulo: "Inventario (stock)",     objeto: "un movimiento de" },
  Mesas:         { modulo: "Mesas",                  objeto: "la mesa" },
  Cuentas:       { modulo: "Cuentas",                objeto: "la cuenta" },
  Pedidos:       { modulo: "Pedidos",                objeto: "el pedido" },
  DetallePedidos:{ modulo: "Pedidos",                objeto: "una línea del pedido" },
  Comprobantes:  { modulo: "Comprobantes de pago",   objeto: "el comprobante de la cuenta" },
  Compras:       { modulo: "Compras",                objeto: "la compra" },
  Roles:         { modulo: "Roles",                  objeto: "el rol" },
  RolesPermisos: { modulo: "Roles y permisos",       objeto: "un permiso del rol" },
  Permisos:      { modulo: "Roles y permisos",       objeto: "el permiso" }
};

/** Nombre técnico del campo → etiqueta visible en el detalle. */
const AUDITORIA_CAMPOS = {
  Usuario: "Usuario", idUsuario: "ID de usuario", nombres: "Nombres", apellidos: "Apellidos",
  documento: "Documento", idTipoDocumento: "Tipo de documento (ID)", telefono: "Teléfono",
  estado: "Estado", idRol: "Rol (ID)", nombreRol: "Rol", email: "Correo electrónico",
  idProveedor: "ID de proveedor", nombreProveedor: "Proveedor", direccion: "Dirección",
  idProducto: "ID de producto", nombreProducto: "Producto", producto: "Producto",
  precioVenta: "Precio de venta", cantidadMinima: "Stock mínimo", stock: "Stock",
  idCategoria: "ID de categoría", nombreCategoria: "Categoría",
  numeroMesa: "Mesa", idCuenta: "ID de cuenta", idPedido: "ID de pedido",
  total: "Total", idMetodo: "Método de pago (ID)", nombreMetodo: "Método de pago",
  idCompra: "ID de compra", productos: "Productos diferentes",
  idPermiso: "ID de permiso", nombrePermiso: "Permiso", Datos: "Datos"
};

/** Campos que se muestran como valores en pesos (COP). */
const AUDITORIA_CAMPOS_COP = ["precioVenta", "total"];

function verboAuditoria(codigo) { return AUDITORIA_VERBOS[codigo] || codigo || "—"; }
function moduloAuditoria(tabla) { return (AUDITORIA_MODULOS[tabla] || {}).modulo || tabla || "—"; }
function etiquetaCampo(k) { return AUDITORIA_CAMPOS[k] || k; }

/** Valor legible: booleanos como Sí/No (o Activo/Inactivo para "estado") y montos en COP. */
function valorCampo(k, v) {
  if (v === "true") return k === "estado" ? "Activo" : "Sí";
  if (v === "false") return k === "estado" ? "Inactivo" : "No";
  if (AUDITORIA_CAMPOS_COP.includes(k) && v !== "" && !isNaN(Number(v))) return formatoCOP(Number(v));
  return v;
}

/** Nombre del registro afectado a partir de sus datos (producto, proveedor, usuario, mesa…). */
function nombreRegistro(datos) {
  if (!datos) return "";
  const d = datos;
  if (d.nombreProducto || d.producto) return d.nombreProducto || d.producto;
  if (d.nombreProveedor) return d.nombreProveedor;
  if (d.nombreCategoria) return d.nombreCategoria;
  if (d.nombreMetodo) return d.nombreMetodo;
  if (d.nombreRol) return d.nombreRol;
  if (d.nombrePermiso) return d.nombrePermiso;
  if (d.nombres) return `${d.nombres} ${d.apellidos || ""}`.trim();
  if (d.numeroMesa) return `Mesa ${d.numeroMesa}`;
  if (d.idCompra) return `N.º ${d.idCompra}`;
  if (d.idCuenta) return `N.º ${d.idCuenta}`;
  if (d.idPedido) return `N.º ${d.idPedido}`;
  if (d.email) return d.email;
  return "";
}

/**
 * Registro afectado, redactado para leerse junto a la acción: "Modificó" · "El producto «Limonada»".
 * @returns {string}
 */
function descripcionAuditoria(accion, tabla, anteriores, nuevos) {
  if (accion === "LOGIN") return "Ingresó al sistema";
  if (accion === "LOGOUT") return "Salió del sistema";
  const info = AUDITORIA_MODULOS[tabla];
  const nombre = nombreRegistro(nuevos) || nombreRegistro(anteriores);
  const objeto = info ? info.objeto : `un registro de ${tabla}`;
  const texto = `${objeto}${nombre ? ` «${nombre}»` : ""}`;
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

const pad2 = (n) => String(n).padStart(2, "0");

/**
 * Fecha del Backend → "AAAA-MM-DDTHH:mm". Las fechas con zona horaria se convierten a hora local.
 * @returns {string} Cadena vacía si el registro no tiene fecha.
 */
function fechaAuditoria(valor) {
  if (!valor) return "";
  const texto = String(valor);
  if (/(Z|[+-]\d{2}:?\d{2})$/.test(texto)) {
    const d = new Date(texto);
    if (!isNaN(d)) return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}T${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
  }
  return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(texto) ? texto.slice(0, 16) : "";
}

/** datosAnteriores/datosNuevos (JSON) → pares clave/valor. Un texto que no es JSON se muestra tal cual. */
function datosAPares(texto) {
  if (texto === null || texto === undefined || texto === "") return undefined;
  try {
    const obj = JSON.parse(texto);
    if (obj && typeof obj === "object" && !Array.isArray(obj)) {
      const salida = {};
      Object.keys(obj).forEach(k => {
        const v = obj[k];
        salida[k] = (v !== null && typeof v === "object") ? JSON.stringify(v) : String(v);
      });
      return salida;
    }
  } catch (e) { }
  return { "Datos": String(texto) };
}

/** Convierte un AuditoriaRespuestaDTO al modelo de la pantalla. */
function adaptarAuditoria(a, nombresUsuarios) {
  const accion = String(a.accion || "").toUpperCase();
  const tabla = a.tabla || "—";
  const anteriores = datosAPares(a.datosAnteriores);
  const nuevos = datosAPares(a.datosNuevos);
  return {
    idAuditoria: a.idAuditoria,
    fecha: fechaAuditoria(a.fecha),
    idUsuario: a.idUsuario,
    usuario: nombresUsuarios[a.idUsuario] || `Usuario #${a.idUsuario}`,
    tabla,
    modulo: moduloAuditoria(tabla),
    accion,
    accionTexto: verboAuditoria(accion),
    detalle: descripcionAuditoria(accion, tabla, anteriores, nuevos),
    anteriores,
    nuevos
  };
}

/** Consulta de auditorías con los nombres de usuario; si no se pueden obtener, se muestra "Usuario #id". */
const apiAuditoria = {
  listar() {
    return Promise.all([
      Api.lista("api/Auditorias/GetAuditorias"),
      Api.lista("api/Usuarios/GetUsuarios").catch(() => [])
    ]).then(([auditorias, usuarios]) => {
      const nombres = {};
      usuarios.forEach(u => { nombres[u.idUsuario] = `${u.nombres} ${u.apellidos}`.trim(); });
      return auditorias.map(a => adaptarAuditoria(a, nombres));
    });
  }
};
