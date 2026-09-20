/* ==========================================================================
   CONFIGURACIÓN — único archivo con direcciones, nombres de endpoints y
   valores de negocio que dependen de la base de datos.
   --------------------------------------------------------------------------
   JavaScript vainilla, sin librerías. Todo cuelga del objeto global `App`.

   ⚠ LO QUE HAY QUE CONFIRMAR CONTRA TU BASE DE DATOS
     Los textos de ESTADOS (más abajo) son cadenas libres en el backend
     (Mesas.estado, Cuentas.estado, Pedidos.estadoPedido, Kardex.tipoMovimiento)
     y no están definidos en ningún archivo .cs. Abre "Configuración →
     Diagnóstico de la API": ahí se listan los valores reales que ya existen
     en tu base para que copies el texto exacto.
   ========================================================================== */
(function (App) {
  "use strict";

  /* ---------- localStorage seguro (falla en modo privado / file://) ---------- */
  function leer(clave) { try { return window.localStorage.getItem(clave); } catch (e) { return null; } }
  function guardar(clave, valor) { try { window.localStorage.setItem(clave, valor); return true; } catch (e) { return false; } }
  function borrar(clave) { try { window.localStorage.removeItem(clave); } catch (e) { /* nada */ } }

  /* ---------- URL del backend ----------
     launchSettings.json: https://localhost:7135  (y http://localhost:5079).
     Se usa HTTPS porque varios controllers llevan [RequireHttps].
     Se puede cambiar sin tocar código en Configuración → URL del backend. */
  var API_POR_DEFECTO = "https://localhost:7135";
  var API_URL = (leer("cuate.apiUrl") || API_POR_DEFECTO).replace(/\/+$/, "");

  /* Las páginas internas viven en /html/, el login en la raíz. */
  var RAIZ = /\/html\/[^\/]*$/.test(window.location.pathname) ? "../" : "";

  /* ---------- Endpoints REALES (copiados de los [HttpGet/Post/Put/Delete]) ----------
     Cada controller usa nombres distintos (GetCategoriaById vs GetProductosById,
     PostCuenta vs PostPedidos…), por eso se listan uno a uno.
     Formato final: {API_URL}/api/{ctl}/{acción}[/{id}]                          */
  var ENTIDADES = {
    auditorias:     { ctl: "Auditorias",     pk: "idAuditoria",     listar: "GetAuditorias",     porId: "GetAuditoriaById",     crear: "PostAuditoria",     editar: "PutAuditoria",     eliminar: "DeleteAuditoria" },
    categorias:     { ctl: "Categorias",     pk: "idCategoria",     listar: "GetCategorias",     porId: "GetCategoriaById",     crear: "PostCategoria",     editar: "PutCategoria",     eliminar: "DeleteCategoria" },
    compras:        { ctl: "Compras",        pk: "idCompra",        listar: "GetCompras",        porId: "GetCompraById",        crear: "PostCompra",        editar: "PutCompra",        eliminar: "DeleteCompra" },
    comprobantes:   { ctl: "Comprobantes",   pk: "idComprobante",   listar: "GetComprobantes",   porId: "GetComprobanteById",   crear: "PostComprobante",   editar: "PutComprobante",   eliminar: "DeleteComprobante" },
    cuentas:        { ctl: "Cuentas",        pk: "idCuenta",        listar: "GetCuentas",        porId: "GetCuentaById",        crear: "PostCuenta",        editar: "PutCuenta",        eliminar: "DeleteCuenta" },
    detalleCompras: { ctl: "DetalleCompras", pk: "idDetalleCompra", listar: "GetDetalleCompras", porId: "GetDetalleCompraById", crear: "PostDetalleCompra", editar: "PutDetalleCompra", eliminar: "DeleteDetalleCompra" },
    detallePedidos: { ctl: "DetallePedidos", pk: "idDetalle",       listar: "GetDetallePedidos", porId: "GetDetallePedidoById", crear: "PostDetallePedido", editar: "PutDetallePedido", eliminar: "DeleteDetallePedido" },
    kardex:         { ctl: "Kardex",         pk: "idMovimiento",    listar: "GetKardex",         porId: "GetKardexById",        crear: "PostKardex",        editar: "PutKardex",        eliminar: "DeleteKardex" },
    /* ⚠ MesasController NO tiene [HttpPost] (aunque MesasRepository.PostMesas existe).
       "PostMesa" es el nombre que habría que crear en el backend; ver informe. */
    mesas:          { ctl: "Mesas",          pk: "idMesa",          listar: "GetMesas",          porId: "GetMesaById",          crear: "PostMesa",          editar: "PutMesa",          eliminar: "DeleteMesa" },
    metodosPago:    { ctl: "MetodosPago",    pk: "idMetodo",        listar: "GetMetodosPago",    porId: "GetMetodosPagoById",   crear: "PostMetodosPago",   editar: "PutMetodosPago",   eliminar: "DeleteMetodosPago" },
    pedidos:        { ctl: "Pedidos",        pk: "idPedido",        listar: "GetPedidos",        porId: "GetPedidosById",       crear: "PostPedidos",       editar: "PutPedidos",       eliminar: "DeletePedidos" },
    permisos:       { ctl: "Permisos",       pk: "idPermiso",       listar: "GetPermisos",       porId: "GetPermisosById",      crear: "PostPermisos",      editar: "PutPermisos",      eliminar: "DeletePermisos" },
    productos:      { ctl: "Productos",      pk: "idProducto",      listar: "GetProductos",      porId: "GetProductosById",     crear: "PostProductos",     editar: "PutProductos",     eliminar: "DeleteProductos" },
    proveedores:    { ctl: "Proveedores",    pk: "idProveedor",     listar: "GetProveedores",    porId: "GetProveedoresById",   crear: "PostProveedores",   editar: "PutProveedores",   eliminar: "DeleteProveedores" },
    roles:          { ctl: "Roles",          pk: "idRol",           listar: "GetRoles",          porId: "GetRolesById",         crear: "PostRoles",         editar: "PutRoles",         eliminar: "DeleteRoles" },
    tipoDocumento:  { ctl: "TipoDocumento",  pk: "idTipoDocumento", listar: "GetTipoDocumento",  porId: "GetTipoDocumentoById", crear: "PostTipoDocumento", editar: "PutTipoDocumento", eliminar: "DeleteTipoDocumento" },
    usuarios:       { ctl: "Usuarios",       pk: "idUsuario",       listar: "GetUsuarios",       porId: "GetUsuariosById",      crear: "PostUsuarios",      editar: "PutUsuarios",      eliminar: "DeleteUsuarios" }
    /* rolesPermisos: RolesPermisosController es un MVC vacío (sin rutas de API);
       la tabla existe en BD pero el frontend no puede leerla ni escribirla. */
  };

  var LOGIN = "/api/Autenticador/Login";

  /* ---------- Valores de texto que dependen de la BD (CONFIRMAR) ---------- */
  var ESTADOS = {
    MESA:   { LIBRE: "Libre",     OCUPADA: "Ocupada" },       // Mesas.estado        (máx. 20)
    CUENTA: { ABIERTA: "Abierta", CERRADA: "Cerrada" },       // Cuentas.estado      (máx. 20)
    PEDIDO: { PENDIENTE: "Pendiente" },                        // Pedidos.estadoPedido (máx. 20)
    KARDEX: { ENTRADA: "Entrada", SALIDA: "Salida" }          // Kardex.tipoMovimiento (máx. 10)
  };

  /* ---------- Comportamientos opcionales ---------- */
  var OPCIONES = {
    /* El backend NO descuenta ni suma stock por sí solo (los POST solo insertan).
       true  → el frontend escribe en Kardex: stock inicial, ajustes, compras (entrada)
               y ventas al pagar (salida).
       false → si tu BD ya lo hace con triggers, ponlo en false para no duplicar. */
    KARDEX_AUTOMATICO: true,

    /* Nada en el backend escribe en la tabla Auditorias. true → el frontend
       registra crear/editar/eliminar con PostAuditoria (nunca bloquea la operación). */
    AUDITORIA_DESDE_FRONTEND: true
  };

  /* ---------- Reglas de validación (alineadas con los modelos .cs) ---------- */
  var REGLAS = {
    PRECIO_MAX: 99999999.99,        // [Precision(10,2)]
    PASSWORD_MIN: 6,
    NOMBRE_MAX: 100,                // Categorias.nombreCategoria [StringLength(100)]
    PROVEEDOR_TEL_MAX: 20,          // Proveedores.telefono [StringLength(20)]
    PROVEEDOR_DIR_MAX: 150,         // Proveedores.direccion [StringLength(150)]
    METODO_MAX: 50,                 // MetodosPago.nombreMetodo [StringLength(50)]
    ROL_MAX: 50,                    // Roles.nombreRol [MaxLength(50)]
    PERMISO_MAX: 100,               // Permisos.nombrePermiso [MaxLength(100)]
    MOTIVO_MAX: 200
  };

  App.config = {
    API_URL: API_URL,
    API_POR_DEFECTO: API_POR_DEFECTO,
    RAIZ: RAIZ,
    LOGIN: LOGIN,
    ENTIDADES: ENTIDADES,
    ESTADOS: ESTADOS,
    OPCIONES: OPCIONES,
    REGLAS: REGLAS,
    almacen: { leer: leer, guardar: guardar, borrar: borrar },

    /* Compara estados sin distinguir mayúsculas/espacios: "ocupada " == "Ocupada". */
    igual: function (a, b) {
      return String(a == null ? "" : a).trim().toLowerCase() === String(b == null ? "" : b).trim().toLowerCase();
    },
    cambiarApiUrl: function (url) {
      var limpia = String(url || "").trim().replace(/\/+$/, "");
      if (!limpia) { borrar("cuate.apiUrl"); return; }
      guardar("cuate.apiUrl", limpia);
    }
  };
})(window.App = window.App || {});
