/* ==========================================================================
   CONFIGURACIÓN — direcciones del backend
   --------------------------------------------------------------------------
   Este es el ÚNICO archivo donde se escribe la URL del backend. Todos los
   demás archivos usan las variables de aquí (URL_PRODUCTOS, URL_MESAS...)
   en vez de escribir la dirección a mano.

   ⚠ ANTES DE PROBAR:
     1. Cambia el puerto si el tuyo es distinto (está en
        Properties/launchSettings.json de tu proyecto .NET).
     2. Verifica que cada nombre de Controller coincida con el tuyo.
        Si tu Controller se llama "ProductoController" (singular), la URL
        es ".../api/Producto", no ".../api/Productos".
   ========================================================================== */

var API_URL = "https://localhost:7135";

var URL_LOGIN          = API_URL + "/api/Autenticador/Login";
var URL_PRODUCTOS      = API_URL + "/api/Productos";
var URL_CATEGORIAS     = API_URL + "/api/Categorias";
var URL_PROVEEDORES    = API_URL + "/api/Proveedores";
var URL_MESAS          = API_URL + "/api/Mesas";
var URL_CUENTAS        = API_URL + "/api/Cuentas";
var URL_PEDIDOS        = API_URL + "/api/Pedidos";
var URL_DETALLE_PEDIDOS = API_URL + "/api/DetallePedidos";
var URL_COMPRAS        = API_URL + "/api/Compras";
var URL_DETALLE_COMPRAS = API_URL + "/api/DetalleCompras";
var URL_COMPROBANTES   = API_URL + "/api/Comprobantes";
var URL_METODOS_PAGO   = API_URL + "/api/MetodosPago";
var URL_USUARIOS       = API_URL + "/api/Usuarios";
var URL_ROLES          = API_URL + "/api/Roles";
var URL_AUDITORIAS     = API_URL + "/api/Auditorias";
var URL_KARDEX          = API_URL + "/api/Kardex";
