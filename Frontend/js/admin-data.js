/* ==========================================================================
   ADMIN-DATA.JS — Capa de datos de Usuarios y Proveedores (El Cuate)
   --------------------------------------------------------------------------
   Consume la API real mediante Api (api-cliente.js). Mantiene la misma
   interfaz que usan los módulos (listar / crear / actualizar / eliminar),
   que devuelven Promises y rechazan con { status, mensaje, errores }.

     apiUsuarios.listar()           GET    api/Usuarios/GetUsuarios
     apiUsuarios.crear(u)           POST   api/Usuarios/PostUsuarios
     apiUsuarios.actualizar(id, u)  PUT    api/Usuarios/PutUsuarios/{id}
     apiUsuarios.eliminar(id)       DELETE api/Usuarios/DeleteUsuarios/{id}

     apiProveedores.listar()        GET    api/Proveedores/GetProveedores
     apiProveedores.crear(p)        POST   api/Proveedores/PostProveedores
     apiProveedores.actualizar(id,p)PUT    api/Proveedores/PutProveedores   (id en el cuerpo)
     apiProveedores.eliminar(id)    DELETE api/Proveedores/DeleteProveedores/{id}

   Requiere: api-cliente.js. Un listado vacío (404 "no se encontraron…") se
   entrega como [] y las respuestas { data } se devuelven ya desempaquetadas.
   ========================================================================== */

/* ---------- Catálogos (se llenan desde la API con cargarCatalogos()) ---------- */
const TIPOS_DOCUMENTO = [];   // { idTipoDocumento, nombre }
const ROLES = [];             // { idRol, nombre }

/** Carga TipoDocumento y Roles desde el Backend y rellena los arreglos anteriores. */
function cargarCatalogos() {
  return Promise.all([
    Api.lista("api/TipoDocumento/GetTipoDocumento"),
    Api.lista("api/Roles/GetRoles")
  ]).then(([tipos, roles]) => {
    TIPOS_DOCUMENTO.length = 0;
    tipos.forEach(t => TIPOS_DOCUMENTO.push({ idTipoDocumento: t.idTipoDocumento, nombre: t.nombreTipo }));
    ROLES.length = 0;
    roles.forEach(r => ROLES.push({ idRol: r.idRol, nombre: r.nombreRol }));
  });
}

const datosDe = (respuesta) => (respuesta && respuesta.data) || null;

/* ---------- API: Usuarios ---------- */
const apiUsuarios = {
  listar() {
    return Api.lista("api/Usuarios/GetUsuarios");
  },
  crear(datos) {
    return Api.post("api/Usuarios/PostUsuarios", datos).then(datosDe).then((u) => {
      Api.auditar("Usuarios", "INSERT", null, u || datos);   // auditar() descarta la contraseña
      return u;
    });
  },
  actualizar(id, datos, anterior) {
    /* UsuarioActualizarDTO no lleva idUsuario ni password: el id viaja en la URL. */
    const { idUsuario, password, ...cuerpo } = datos;
    return Api.put("api/Usuarios/PutUsuarios/" + id, cuerpo).then(datosDe).then((u) => {
      Api.auditar("Usuarios", "UPDATE", anterior || { idUsuario: id }, u || { idUsuario: id, ...cuerpo });
      return u;
    });
  },
  eliminar(id, anterior) {
    return Api.del("api/Usuarios/DeleteUsuarios/" + id).then(() => {
      Api.auditar("Usuarios", "DELETE", anterior || { idUsuario: id }, null);
      return true;
    });
  }
};

/* ---------- API: Proveedores ---------- */
const apiProveedores = {
  listar() {
    return Api.lista("api/Proveedores/GetProveedores");
  },
  crear(datos) {
    return Api.post("api/Proveedores/PostProveedores", datos).then(datosDe).then((p) => {
      Api.auditar("Proveedores", "INSERT", null, p || datos);
      return p;
    });
  },
  actualizar(id, datos, anterior) {
    return Api.put("api/Proveedores/PutProveedores", { ...datos, idProveedor: id }).then(datosDe).then((p) => {
      Api.auditar("Proveedores", "UPDATE", anterior || { idProveedor: id }, p || { ...datos, idProveedor: id });
      return p;
    });
  },
  eliminar(id, anterior) {
    return Api.del("api/Proveedores/DeleteProveedores/" + id).then(() => {
      Api.auditar("Proveedores", "DELETE", anterior || { idProveedor: id }, null);
      return true;
    });
  }
};
