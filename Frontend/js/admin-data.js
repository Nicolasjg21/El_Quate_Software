/* ==========================================================================
   ADMIN-DATA.JS — Capa de datos de Usuarios y Proveedores (El Cuate)
   --------------------------------------------------------------------------
   Simula el backend con localStorage y expone una API asíncrona (Promises)
   con las 4 operaciones del CRUD. Cuando existan los endpoints reales, solo
   hay que reemplazar el cuerpo de estas funciones (los módulos no se tocan):

     apiUsuarios.listar()            ->  GET    <endpoint real>
     apiUsuarios.crear(u)            ->  POST   <endpoint real>
     apiUsuarios.actualizar(id, u)   ->  PUT    <endpoint real>/{id}
     apiUsuarios.eliminar(id)        ->  DELETE <endpoint real>/{id}
     (igual para apiProveedores)

   Los errores se rechazan como { status, mensaje } (409 = registros asociados).
   Estructura de datos = modelos C# oficiales (Usuarios / Proveedores).
   ========================================================================== */

const ADMIN_DB = {
  USUARIOS: "elcuate_usuarios",
  USUARIOS_SEQ: "elcuate_usuarios_seq",
  PROVEEDORES: "elcuate_proveedores",
  PROVEEDORES_SEQ: "elcuate_proveedores_seq",
  VERSION: "elcuate_admin_db_version"
};
const ADMIN_DB_VERSION = 1;
const LATENCIA_SIMULADA_MS = 350;

/* ---------- Catálogos (relaciones TipoDocumento y Roles) ---------- */
const TIPOS_DOCUMENTO = [
  { idTipoDocumento: 1, nombre: "Cédula de Ciudadanía" },
  { idTipoDocumento: 2, nombre: "Cédula de Extranjería" },
  { idTipoDocumento: 3, nombre: "Tarjeta de Identidad" }
];

const ROLES = [
  { idRol: 1, nombre: "Administrador" },
  { idRol: 2, nombre: "Caja" },
  { idRol: 3, nombre: "Mesero" }
];

/* ---------- Semillas ---------- */
const USUARIOS_SEMILLA = [
  { idUsuario: 1,  nombres: "María",        apellidos: "García",     documento: "1023456789", idTipoDocumento: 1, telefono: "310 456 7821", estado: true,  idRol: 1, email: "maria.garcia@gmail.com" },
  { idUsuario: 2,  nombres: "Carlos",       apellidos: "Rodríguez",  documento: "1034567890", idTipoDocumento: 1, telefono: "315 628 4190", estado: true,  idRol: 2, email: "carlos.rodriguez@gmail.com" },
  { idUsuario: 3,  nombres: "Juan",         apellidos: "Torres",     documento: "1019876543", idTipoDocumento: 1, telefono: "300 751 2846", estado: true,  idRol: 3, email: "juan.torres@gmail.com" },
  { idUsuario: 4,  nombres: "Ana",          apellidos: "Silva",      documento: "52234567",   idTipoDocumento: 1, telefono: "311 904 5632", estado: false, idRol: 2, email: "ana.silva@gmail.com" },
  { idUsuario: 5,  nombres: "Luis",         apellidos: "Moreno",     documento: "1045678901", idTipoDocumento: 1, telefono: "316 285 7401", estado: true,  idRol: 3, email: "luis.moreno@gmail.com" },
  { idUsuario: 6,  nombres: "Sofía",        apellidos: "Méndez",     documento: "1056789012", idTipoDocumento: 1, telefono: "301 648 9320", estado: true,  idRol: 1, email: "sofia.mendez@gmail.com" },
  { idUsuario: 7,  nombres: "David",        apellidos: "Cruz",       documento: "1067890123", idTipoDocumento: 1, telefono: "304 816 2375", estado: false, idRol: 2, email: "david.cruz@gmail.com" },
  { idUsuario: 8,  nombres: "Elena",        apellidos: "Gómez",      documento: "1078901234", idTipoDocumento: 1, telefono: "320 764 1985", estado: true,  idRol: 3, email: "elena.gomez@gmail.com" },
  { idUsuario: 9,  nombres: "Juan Camilo",  apellidos: "Restrepo",   documento: "1089012345", idTipoDocumento: 1, telefono: "300 418 5627", estado: true,  idRol: 1, email: "juan.restrepo@gmail.com" },
  { idUsuario: 10, nombres: "Laura",        apellidos: "Rodríguez",  documento: "1090123456", idTipoDocumento: 1, telefono: "310 275 8463", estado: true,  idRol: 2, email: "laura.rodriguez@gmail.com" }
];

const PROVEEDORES_SEMILLA = [
  { idProveedor: 1,  nombreProveedor: "Central Cervecera de Colombia", telefono: "+57 310 456 7821", direccion: "Calle 80 # 68-45, Bogotá" },
  { idProveedor: 2,  nombreProveedor: "Distribuidora Bavaria",         telefono: "+57 315 628 4190", direccion: "Carrera 13 # 63-18, Bogotá" },
  { idProveedor: 3,  nombreProveedor: "Licores Premium SAS",           telefono: "+57 300 751 2846", direccion: "Calle 26 # 68-35, Bogotá" },
  { idProveedor: 4,  nombreProveedor: "Distribuciones El Barril",      telefono: "+57 311 904 5632", direccion: "Carrera 7 # 72-41, Bogotá" },
  { idProveedor: 5,  nombreProveedor: "Comercializadora Andina",       telefono: "+57 316 285 7401", direccion: "Calle 93 # 14-28, Bogotá" },
  { idProveedor: 6,  nombreProveedor: "Licores del Valle",             telefono: "+57 301 648 9320", direccion: "Carrera 5 # 10-36, Cali" },
  { idProveedor: 7,  nombreProveedor: "Importadora Premium Spirits",   telefono: "+57 318 472 6158", direccion: "Calle 17 # 52-08, Medellín" },
  { idProveedor: 8,  nombreProveedor: "Distribuidora Antioquia",       telefono: "+57 304 816 2375", direccion: "Carrera 43A # 16-42, Medellín" },
  { idProveedor: 9,  nombreProveedor: "Bodega Central",                telefono: "+57 312 539 8046", direccion: "Calle 12 # 32-18, Bogotá" },
  { idProveedor: 10, nombreProveedor: "Grupo Licorero Nacional",       telefono: "+57 320 764 1985", direccion: "Carrera 48 # 24-16, Barranquilla" }
];

/* ---------- Almacenamiento ---------- */
function inicializarAdminDB() {
  if (localStorage.getItem(ADMIN_DB.VERSION) !== String(ADMIN_DB_VERSION)) {
    localStorage.setItem(ADMIN_DB.USUARIOS, JSON.stringify(USUARIOS_SEMILLA));
    localStorage.setItem(ADMIN_DB.USUARIOS_SEQ, String(USUARIOS_SEMILLA.length));
    localStorage.setItem(ADMIN_DB.PROVEEDORES, JSON.stringify(PROVEEDORES_SEMILLA));
    localStorage.setItem(ADMIN_DB.PROVEEDORES_SEQ, String(PROVEEDORES_SEMILLA.length));
    localStorage.setItem(ADMIN_DB.VERSION, String(ADMIN_DB_VERSION));
  }
}

function leerTabla(clave) { return JSON.parse(localStorage.getItem(clave) || "[]"); }
function escribirTabla(clave, filas) { localStorage.setItem(clave, JSON.stringify(filas)); }

function siguienteId(claveSeq) {
  const n = parseInt(localStorage.getItem(claveSeq) || "0", 10) + 1;
  localStorage.setItem(claveSeq, String(n));
  return n;
}

/** Envuelve una operación síncrona en una Promise con latencia simulada. */
function simularRespuesta(operacion) {
  return new Promise((resolver, rechazar) => {
    setTimeout(() => {
      try { resolver(operacion()); }
      catch (err) { rechazar(err); }
    }, LATENCIA_SIMULADA_MS);
  });
}

function errorApi(status, mensaje) { return { status, mensaje }; }

/* ---------- API: Usuarios ---------- */
const apiUsuarios = {
  listar() {
    return simularRespuesta(() => leerTabla(ADMIN_DB.USUARIOS));
  },
  crear(datos) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.USUARIOS);
      const nuevo = { ...datos, idUsuario: siguienteId(ADMIN_DB.USUARIOS_SEQ) };
      filas.push(nuevo);
      escribirTabla(ADMIN_DB.USUARIOS, filas);
      return nuevo;
    });
  },
  actualizar(id, datos) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.USUARIOS);
      const idx = filas.findIndex(u => u.idUsuario === id);
      if (idx === -1) throw errorApi(404, "El usuario ya no existe.");
      filas[idx] = { ...datos, idUsuario: id };
      escribirTabla(ADMIN_DB.USUARIOS, filas);
      return filas[idx];
    });
  },
  eliminar(id) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.USUARIOS);
      if (!filas.some(u => u.idUsuario === id)) throw errorApi(404, "El usuario ya no existe.");
      /* Un backend real responde 409 si hay Pedidos / Kardex / Auditorías asociados.
         Aquí no existen esas tablas, así que la eliminación siempre procede. */
      escribirTabla(ADMIN_DB.USUARIOS, filas.filter(u => u.idUsuario !== id));
      return true;
    });
  }
};

/* ---------- API: Proveedores ---------- */
const apiProveedores = {
  listar() {
    return simularRespuesta(() => leerTabla(ADMIN_DB.PROVEEDORES));
  },
  crear(datos) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.PROVEEDORES);
      const nuevo = { ...datos, idProveedor: siguienteId(ADMIN_DB.PROVEEDORES_SEQ) };
      filas.push(nuevo);
      escribirTabla(ADMIN_DB.PROVEEDORES, filas);
      return nuevo;
    });
  },
  actualizar(id, datos) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.PROVEEDORES);
      const idx = filas.findIndex(p => p.idProveedor === id);
      if (idx === -1) throw errorApi(404, "El proveedor ya no existe.");
      filas[idx] = { ...datos, idProveedor: id };
      escribirTabla(ADMIN_DB.PROVEEDORES, filas);
      return filas[idx];
    });
  },
  eliminar(id) {
    return simularRespuesta(() => {
      const filas = leerTabla(ADMIN_DB.PROVEEDORES);
      const prov = filas.find(p => p.idProveedor === id);
      if (!prov) throw errorApi(404, "El proveedor ya no existe.");
      /* Simulación de integridad referencial: si el inventario referencia al
         proveedor (compras asociadas), el "backend" rechaza con 409. */
      const conCompras = (typeof obtenerProductos === "function") &&
        obtenerProductos().some(p => (p.proveedor || "").trim().toLowerCase() === prov.nombreProveedor.trim().toLowerCase());
      if (conCompras) throw errorApi(409, "conflicto");
      escribirTabla(ADMIN_DB.PROVEEDORES, filas.filter(p => p.idProveedor !== id));
      return true;
    });
  }
};

inicializarAdminDB();
