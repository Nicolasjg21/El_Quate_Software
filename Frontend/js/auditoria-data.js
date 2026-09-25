/* ==========================================================================
   AUDITORIA-DATA.JS — Capa de consulta de Auditoría (El Cuate)
   --------------------------------------------------------------------------
   Auditoría es SOLO CONSULTA: los registros los genera el backend cuando
   ocurren operaciones. Aquí no existe crear / editar / eliminar.

   Cuando exista el endpoint real, solo hay que reemplazar el cuerpo de
   apiAuditoria.listar() por la llamada GET correspondiente.

   IMPORTANTE: los registros de abajo son DATOS DE DEMOSTRACIÓN, no provienen
   de producción. Respetan la separación de roles:
     - Patricia (Administradora): operaciones administrativas.
     - Empleados: solo operaciones operativas (Mesas, Pedidos, Comprobantes)
       y acceso (LOGIN / LOGOUT).
   ========================================================================== */

const AUDITORIA_ACCIONES = ["INSERT", "UPDATE", "DELETE", "LOGIN", "LOGOUT"];
const AUDITORIA_TABLAS = ["Productos", "Pedidos", "Mesas", "Compras", "Usuarios", "Proveedores", "Inventario", "Comprobantes"];

/* fecha: "AAAA-MM-DDTHH:mm" (hora local). anteriores / nuevos: pares etiqueta → valor. */
const AUDITORIA_SEMILLA = [
  { idAuditoria: 15, fecha: "2026-09-23T14:32", usuario: "Patricia", tabla: "Productos", modulo: "Productos", accion: "UPDATE",
    detalle: "Actualización de producto",
    anteriores: { "Nombre": "Club Colombia Dorada", "Precio": "$ 5.500", "Estado": "Activo" },
    nuevos:     { "Nombre": "Club Colombia Dorada", "Precio": "$ 6.000", "Estado": "Activo" } },
  { idAuditoria: 14, fecha: "2026-09-23T14:18", usuario: "Patricia", tabla: "Productos", modulo: "Productos", accion: "INSERT",
    detalle: "Creación del producto Aguardiente Antioqueño Verde.",
    nuevos: { "Nombre": "Aguardiente Antioqueño Verde", "Categoría": "Licores", "Precio de venta": "$ 19.000", "Estado": "Activo" } },
  { idAuditoria: 13, fecha: "2026-09-23T13:47", usuario: "Carlos Ramírez", tabla: "Pedidos", modulo: "Pedidos", accion: "INSERT",
    detalle: "Creación de pedido para Mesa 3.",
    nuevos: { "Mesa": "3", "Mesero": "Carlos Ramírez", "Productos": "Club Colombia Dorada × 2, Red Bull × 1" } },
  { idAuditoria: 12, fecha: "2026-09-23T13:39", usuario: "Andrés Gómez", tabla: "Mesas", modulo: "Mesas", accion: "UPDATE",
    detalle: "Actualización del estado de la Mesa 5.",
    anteriores: { "Estado": "Libre" }, nuevos: { "Estado": "Ocupada" } },
  { idAuditoria: 11, fecha: "2026-09-23T13:25", usuario: "Carlos Ramírez", tabla: "Comprobantes", modulo: "Comprobantes", accion: "INSERT",
    detalle: "Generación de comprobante correspondiente a una orden pagada.",
    nuevos: { "Mesa": "5", "Total pagado": "$ 148.000", "Método de pago": "Efectivo" } },
  { idAuditoria: 10, fecha: "2026-09-23T12:51", usuario: "Patricia", tabla: "Proveedores", modulo: "Proveedores", accion: "UPDATE",
    detalle: "Actualización de información del proveedor.",
    anteriores: { "Teléfono": "310 456 7821", "Dirección": "Calle 80 # 68-45, Bogotá" },
    nuevos:     { "Teléfono": "315 628 4190", "Dirección": "Calle 80 # 68-45, Bogotá" } },
  { idAuditoria: 9, fecha: "2026-09-23T11:42", usuario: "Patricia", tabla: "Usuarios", modulo: "Usuarios", accion: "INSERT",
    detalle: "Creación de nuevo usuario.",
    nuevos: { "Nombres": "Carlos", "Apellidos": "Ramírez", "Rol": "Mesero", "Estado": "Activo" } },
  { idAuditoria: 8, fecha: "2026-09-23T10:35", usuario: "María López", tabla: "Pedidos", modulo: "Pedidos", accion: "UPDATE",
    detalle: "Actualización del pedido de la Mesa 3.",
    anteriores: { "Estado": "En curso", "Cantidad": "2" }, nuevos: { "Estado": "Listo", "Cantidad": "3" } },
  { idAuditoria: 7, fecha: "2026-09-23T09:48", usuario: "Patricia", tabla: "Inventario", modulo: "Inventario", accion: "UPDATE",
    detalle: "Actualización del stock mínimo de un producto.",
    anteriores: { "Producto": "Corona Extra 330 ml", "Stock mínimo": "15" },
    nuevos:     { "Producto": "Corona Extra 330 ml", "Stock mínimo": "20" } },
  { idAuditoria: 6, fecha: "2026-09-23T08:15", usuario: "Patricia", tabla: "Productos", modulo: "Productos", accion: "DELETE",
    detalle: "Eliminación del producto.",
    anteriores: { "Nombre": "Producto retirado", "Precio": "$ 12.000", "Estado": "Inactivo" } },
  { idAuditoria: 5, fecha: "2026-09-23T08:01", usuario: "Patricia", tabla: "Usuarios", modulo: "Usuarios", accion: "LOGIN",
    detalle: "Inicio de sesión.",
    nuevos: { "Usuario": "Patricia", "Rol": "Administrador" } },
  { idAuditoria: 4, fecha: "2026-09-22T22:10", usuario: "Carlos Ramírez", tabla: "Usuarios", modulo: "Usuarios", accion: "LOGOUT",
    detalle: "Cierre de sesión.",
    nuevos: { "Usuario": "Carlos Ramírez", "Rol": "Mesero" } },
  { idAuditoria: 3, fecha: "2026-09-22T18:04", usuario: "Carlos Ramírez", tabla: "Usuarios", modulo: "Usuarios", accion: "LOGIN",
    detalle: "Inicio de sesión.",
    nuevos: { "Usuario": "Carlos Ramírez", "Rol": "Mesero" } },
  { idAuditoria: 2, fecha: "2026-09-21T20:15", usuario: "Patricia", tabla: "Compras", modulo: "Compras", accion: "INSERT",
    detalle: "Registro de compra a proveedor.",
    nuevos: { "Proveedor": "Distribuidora Bavaria", "Productos": "Águila Original × 48, Poker × 24", "Costo total": "$ 282.000" } },
  { idAuditoria: 1, fecha: "2026-09-21T19:40", usuario: "Andrés Gómez", tabla: "Mesas", modulo: "Mesas", accion: "UPDATE",
    detalle: "Actualización del estado de la Mesa 2.",
    anteriores: { "Estado": "Ocupada" }, nuevos: { "Estado": "Libre" } }
];

const LATENCIA_AUDITORIA_MS = 350;

const apiAuditoria = {
  /* GET — pendiente de conectar al endpoint real. */
  listar() {
    return new Promise((resolver) => {
      setTimeout(() => resolver(AUDITORIA_SEMILLA.map(r => ({ ...r }))), LATENCIA_AUDITORIA_MS);
    });
  }
};
