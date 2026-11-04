/**
 * @file admin-data.js
 * @description Acceso a datos de Usuarios, Proveedores y Seguridad (roles y permisos).
 *   Cada operación de escritura registra su auditoría.
 * @requires api-cliente.js
 *
 * Endpoints:
 *   Usuarios      GetUsuarios, PostUsuarios, PutUsuarios/{id}, DeleteUsuarios/{id}
 *   Proveedores   GetProveedores, PostProveedores, PutProveedores (id en el cuerpo), DeleteProveedores/{id}
 *   Catálogos     TipoDocumento/GetTipoDocumento, Roles/GetRoles
 *   Seguridad     Roles/PostRoles, RolesPermisos/PostRolesPermisos, RolesPermisos/DeleteRolesPermisos/{idRol}/{idPermiso}
 */

/** Catálogos cargados por cargarCatalogos(). */
const TIPOS_DOCUMENTO = [];
const ROLES = [];

/** Carga los tipos de documento y los roles en TIPOS_DOCUMENTO y ROLES. */
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

/** Extrae "data" de una respuesta de la API. */
const datosDe = (respuesta) => (respuesta && respuesta.data) || null;

/** Usuarios. La contraseña solo se envía al crear; se cambia con CambiarPassword. */
const apiUsuarios = {
  listar() {
    return Api.lista("api/Usuarios/GetUsuarios");
  },
  crear(datos) {
    return Api.post("api/Usuarios/PostUsuarios", datos).then(datosDe).then((u) => {
      Api.auditar("Usuarios", "INSERT", null, u || datos);
      return u;
    });
  },
  actualizar(id, datos, anterior) {
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

/** Proveedores. */
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

/**
 * Roles y permisos (requiere "seguridad.gestionar" en el Backend).
 * Los permisos se asignan por rol mediante la tabla RolesPermisos; no existen permisos por usuario.
 */
const apiSeguridad = {
  cargar() {
    return Promise.all([
      Api.lista("api/Roles/GetRoles"),
      Api.lista("api/Permisos/GetPermisos"),
      Api.lista("api/RolesPermisos/GetRolesPermisos")
    ]).then(([roles, permisos, relaciones]) => ({ roles, permisos, relaciones }));
  },
  crearRol(nombreRol) {
    return Api.post("api/Roles/PostRoles", { nombreRol }).then(datosDe).then((r) => {
      Api.auditar("Roles", "INSERT", null, r || { nombreRol });
      return r;
    });
  },
  asignar(rol, permiso) {
    return Api.post("api/RolesPermisos/PostRolesPermisos", { idRol: rol.idRol, idPermiso: permiso.idPermiso }).then(() => {
      Api.auditar("RolesPermisos", "INSERT", null, { idRol: rol.idRol, nombreRol: rol.nombreRol, idPermiso: permiso.idPermiso, nombrePermiso: permiso.nombrePermiso });
    });
  },
  quitar(rol, permiso) {
    return Api.del(`api/RolesPermisos/DeleteRolesPermisos/${rol.idRol}/${permiso.idPermiso}`).then(() => {
      Api.auditar("RolesPermisos", "DELETE", { idRol: rol.idRol, nombreRol: rol.nombreRol, idPermiso: permiso.idPermiso, nombrePermiso: permiso.nombrePermiso }, null);
    });
  }
};
