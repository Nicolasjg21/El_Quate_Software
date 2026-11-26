using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Controllers
{
    /// <summary>
    /// Gestión de usuarios (bloque 2.12).
    /// - Quién puede hacer qué lo decide ReglasUsuarios, en el servidor y antes de tocar la base.
    /// - Cada cambio se guarda junto con su auditoría en una sola transacción; el usuario que
    ///   aparece en la auditoría sale del token, nunca del cuerpo de la petición.
    /// - Nadie cambia sus propios datos de identidad, rol ni estado: cada usuario cambia solo
    ///   su teléfono (MiPerfil) y su contraseña (CambiarPassword).
    /// - Restablecer contraseña genera una clave temporal de un solo uso que vence en 24 horas
    ///   y obliga a cambiarla al entrar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private const string tablaAuditoria = "Usuarios";

        private readonly IUsuariosRepository usuariosRepository;
        private readonly IPasswordHasher<Usuarios> passwordHasher;
        private readonly IAuthorizationService authorizationService;
        private readonly ITokenService tokenService;
        private readonly IPermisosRolService permisosRolService;
        private readonly ReglasUsuarios reglas;
        private readonly ILogger<UsuariosController> logger;

        public UsuariosController(
            IUsuariosRepository repository,
            IPasswordHasher<Usuarios> passwordHasher,
            IAuthorizationService authorizationService,
            ITokenService tokenService,
            IPermisosRolService permisosRolService,
            ReglasUsuarios reglas,
            ILogger<UsuariosController> logger)
        {
            usuariosRepository = repository;
            this.passwordHasher = passwordHasher;
            this.authorizationService = authorizationService;
            this.tokenService = tokenService;
            this.permisosRolService = permisosRolService;
            this.reglas = reglas;
            this.logger = logger;
        }

        // GET: api/Usuarios/GetUsuarios
        /// <summary>Listado para la pantalla Usuarios (usuarios.gestionar) y para Auditoría (auditorias.consultar).</summary>
        [HttpGet("GetUsuarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var gestiona = await TienePermiso(PermisosSistema.usuariosGestionar);

                if (!gestiona && !await TienePermiso(PermisosSistema.auditoriasConsultar))
                {
                    return Prohibido("No tiene permisos para consultar los usuarios.");
                }

                var usuarios = await usuariosRepository.GetUsuarios();

                if (usuarios == null || usuarios.Count == 0)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron usuarios."
                    });
                }

                var data = new List<UsuarioConAccionesDTO>();
                var rolesAdministrador = new Dictionary<int, bool>();

                foreach (var u in usuarios)
                {
                    if (!rolesAdministrador.TryGetValue(u.idRol, out var esAdministrador))
                    {
                        esAdministrador = await RolEsAdministrador(u.idRol);
                        rolesAdministrador[u.idRol] = esAdministrador;
                    }

                    data.Add(u.ToConAccionesDTO(new AccionesUsuarioDTO
                    {
                        editar = gestiona && reglas.Editar(idActor.Value, u.idUsuario, esAdministrador, esAdministrador) == null,
                        eliminar = gestiona && reglas.Eliminar(idActor.Value, u.idUsuario) == null,
                        restablecerPassword = gestiona && reglas.Restablecer(idActor.Value, u.idUsuario) == null
                    }));
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuarios obtenidos correctamente.",
                    data
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al obtener los usuarios.");
            }
        }

        // GET: api/Usuarios/GetUsuariosById/1
        /// <summary>El propio usuario o quien tenga usuarios.gestionar.</summary>
        [HttpGet("GetUsuariosById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsuariosById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return IdInvalido();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                if (idActor.Value != id && !await TienePermiso(PermisosSistema.usuariosGestionar))
                {
                    return Prohibido("No tiene permisos para consultar otros usuarios.");
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario obtenido correctamente.",
                    data = usuario.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al obtener el usuario.");
            }
        }

        // GET: api/Usuarios/MiPerfil
        /// <summary>Datos del usuario de la sesión y si puede editar su teléfono.</summary>
        [HttpGet("MiPerfil")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMiPerfil()
        {
            try
            {
                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var usuario = await usuariosRepository.GetUsuariosById(idActor.Value);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Perfil obtenido correctamente.",
                    data = usuario.ToRespuestaDTO(),
                    puedeEditarTelefono = reglas.EditarPerfilPropio(idActor.Value) == null
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al obtener el perfil.");
            }
        }

        // PUT: api/Usuarios/MiPerfil
        /// <summary>El usuario de la sesión cambia su teléfono. Nombres, documento, correo, rol y estado no.</summary>
        [HttpPut("MiPerfil")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutMiPerfil([FromBody] MiPerfilActualizarDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return DatosInvalidos();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var motivo = reglas.EditarPerfilPropio(idActor.Value);

                if (motivo != null)
                {
                    return Prohibido(motivo);
                }

                var usuario = await usuariosRepository.GetUsuariosById(idActor.Value);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                var antes = usuario.ToAuditoria();
                usuario.telefono = dto.telefono.Trim();

                await usuariosRepository.ActualizarConAuditoria(
                    usuario, Auditoria("UPDATE", idActor.Value, antes, usuario.ToAuditoria()));

                return Ok(new
                {
                    statusCode = 200,
                    message = "Teléfono actualizado correctamente.",
                    data = usuario.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al actualizar el perfil.");
            }
        }

        // POST: api/Usuarios/PostUsuarios
        [HttpPost("PostUsuarios")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostUsuarios([FromBody] UsuarioCrearDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return DatosInvalidos();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var motivo = reglas.Crear(idActor.Value, await RolEsAdministrador(dto.idRol));

                if (motivo != null)
                {
                    return Prohibido(motivo);
                }

                var email = dto.email.Trim();

                if (await usuariosRepository.ExisteEmail(email))
                {
                    return CorreoRepetido();
                }

                var usuario = new Usuarios
                {
                    nombres = dto.nombres.Trim(),
                    apellidos = dto.apellidos.Trim(),
                    documento = dto.documento.Trim(),
                    idTipoDocumento = dto.idTipoDocumento,
                    telefono = dto.telefono.Trim(),
                    estado = dto.estado,
                    idRol = dto.idRol,
                    email = email
                };

                usuario.passwordHash = passwordHasher.HashPassword(usuario, dto.password);

                var guardado = await usuariosRepository.CrearConAuditoria(
                    usuario, Auditoria("INSERT", idActor.Value, null, usuario.ToAuditoria()));

                if (!guardado)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el usuario."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Usuario registrado correctamente.",
                    data = usuario.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                return ConflictoIntegridad(ex);
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al registrar el usuario.");
            }
        }

        // PUT: api/Usuarios/PutUsuarios/1
        [HttpPut("PutUsuarios/{id}")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutUsuarios(int id, [FromBody] UsuarioActualizarDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return DatosInvalidos();
                }

                if (id <= 0)
                {
                    return IdInvalido();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var existente = await usuariosRepository.GetUsuariosById(id);

                if (existente == null)
                {
                    return NoEncontrado();
                }

                var motivo = reglas.Editar(
                    idActor.Value,
                    id,
                    await RolEsAdministrador(existente.idRol),
                    await RolEsAdministrador(dto.idRol));

                if (motivo != null)
                {
                    return Prohibido(motivo);
                }

                var email = dto.email.Trim();

                if (await usuariosRepository.ExisteEmail(email, id))
                {
                    return CorreoRepetido();
                }

                var antes = existente.ToAuditoria();

                existente.nombres = dto.nombres.Trim();
                existente.apellidos = dto.apellidos.Trim();
                existente.documento = dto.documento.Trim();
                existente.idTipoDocumento = dto.idTipoDocumento;
                existente.telefono = dto.telefono.Trim();
                existente.estado = dto.estado;
                existente.idRol = dto.idRol;
                existente.email = email;

                var guardado = await usuariosRepository.ActualizarConAuditoria(
                    existente, Auditoria("UPDATE", idActor.Value, antes, existente.ToAuditoria()));

                if (!guardado)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el usuario."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario actualizado correctamente.",
                    data = existente.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                return ConflictoIntegridad(ex);
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al actualizar el usuario.");
            }
        }

        // PUT: api/Usuarios/CambiarPassword/1
        /// <summary>
        /// El usuario de la sesión cambia su propia contraseña (también la temporal). Para la de otro
        /// usuario se usa RestablecerPassword. Devuelve un token nuevo porque el anterior deja de servir.
        /// </summary>
        [HttpPut("CambiarPassword/{id}")]
        [PermitidoConCambioPendiente]
        [EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CambiarPassword(int id, [FromBody] CambiarPasswordDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return DatosInvalidos();
                }

                if (id <= 0)
                {
                    return IdInvalido();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                if (idActor.Value != id)
                {
                    return Prohibido("Solo puede cambiar su propia contraseña. Para la de otro usuario use Restablecer contraseña.");
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                var eraTemporal = usuario.debeCambiarPassword;

                if (eraTemporal &&
                    (usuario.passwordTemporalExpira == null || usuario.passwordTemporalExpira <= DateTime.UtcNow))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La contraseña temporal venció. Solicite a la administración un nuevo restablecimiento."
                    });
                }

                var verificacion = passwordHasher.VerifyHashedPassword(usuario, usuario.passwordHash, dto.passwordActual);

                if (verificacion == PasswordVerificationResult.Failed)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = eraTemporal ? "La contraseña temporal es incorrecta." : "La contraseña actual es incorrecta."
                    });
                }

                if (dto.nuevaPassword != dto.confirmarPassword)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La nueva contraseña y la confirmación no coinciden."
                    });
                }

                if (dto.nuevaPassword == dto.passwordActual)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La nueva contraseña debe ser distinta de la actual."
                    });
                }

                usuario.passwordHash = passwordHasher.HashPassword(usuario, dto.nuevaPassword);
                usuario.debeCambiarPassword = false;
                usuario.passwordTemporalExpira = null;

                var detalle = eraTemporal ? "Reemplazó su contraseña temporal" : "Cambió su contraseña";

                var guardado = await usuariosRepository.ActualizarConAuditoria(
                    usuario, Auditoria("CAMBIAR_PASSWORD", idActor.Value, null, usuario.ToAuditoriaPassword(detalle)));

                if (!guardado)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la contraseña."
                    });
                }

                // El sello del token deriva del hash: el token anterior ya no sirve y se entrega uno nuevo.
                var nuevoToken = tokenService.GenerarToken(usuario);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Contraseña actualizada correctamente.",
                    token = nuevoToken.token,
                    expiraEn = nuevoToken.expiraEn
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al actualizar la contraseña.");
            }
        }

        // POST: api/Usuarios/RestablecerPassword/1
        /// <summary>
        /// Soporte técnico o la administradora principal asignan una contraseña temporal aleatoria,
        /// de un solo uso, que vence en 24 horas. Se muestra una sola vez y cierra todas las sesiones
        /// del usuario. Al entrar, el usuario queda bloqueado hasta cambiarla.
        /// </summary>
        [HttpPost("RestablecerPassword/{id}")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestablecerPassword(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return IdInvalido();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var motivo = reglas.Restablecer(idActor.Value, id);

                if (motivo != null)
                {
                    return Prohibido(motivo);
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                var temporal = GeneradorPasswordTemporal.Generar();
                var expiraEn = DateTime.UtcNow.Add(GeneradorPasswordTemporal.vigencia);

                usuario.passwordHash = passwordHasher.HashPassword(usuario, temporal);
                usuario.debeCambiarPassword = true;
                usuario.passwordTemporalExpira = expiraEn;

                var detalle = $"Contraseña temporal asignada; vence {expiraEn:yyyy-MM-dd HH:mm} UTC";

                var guardado = await usuariosRepository.ActualizarConAuditoria(
                    usuario, Auditoria("RESTABLECER_PASSWORD", idActor.Value, null, usuario.ToAuditoriaPassword(detalle)));

                if (!guardado)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible restablecer la contraseña."
                    });
                }

                logger.LogInformation("Usuario {IdActor} restableció la contraseña del usuario {IdUsuario}", idActor.Value, id);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Contraseña restablecida. Entregue la contraseña temporal al usuario; no se volverá a mostrar.",
                    passwordTemporal = temporal,
                    expiraEn
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al restablecer la contraseña.");
            }
        }

        // DELETE: api/Usuarios/DeleteUsuarios/1
        /// <summary>
        /// Solo Soporte técnico o la administradora principal. Un usuario con movimientos no se
        /// elimina (409): se desactiva, para conservar la auditoría.
        /// </summary>
        [HttpDelete("DeleteUsuarios/{id}")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUsuarios(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return IdInvalido();
                }

                var idActor = User.ObtenerIdUsuario();

                if (idActor == null)
                {
                    return SinSesion();
                }

                var motivo = reglas.Eliminar(idActor.Value, id);

                if (motivo != null)
                {
                    return Prohibido(motivo);
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NoEncontrado();
                }

                var eliminado = await usuariosRepository.EliminarConAuditoria(
                    usuario, Auditoria("DELETE", idActor.Value, usuario.ToAuditoria(), null));

                if (!eliminado)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el usuario."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario eliminado correctamente."
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "No se pudo eliminar el usuario {IdUsuario} por registros asociados", id);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "No es posible eliminar el usuario porque tiene registros asociados. Puede desactivarlo."
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al eliminar el usuario.");
            }
        }

        // ---------- Auxiliares ----------

        private async Task<bool> TienePermiso(string permiso)
        {
            return (await authorizationService.AuthorizeAsync(User, PoliticasAutorizacion.Permiso(permiso))).Succeeded;
        }

        /// <summary>Un rol es de administrador si puede gestionar usuarios.</summary>
        private Task<bool> RolEsAdministrador(int idRol)
        {
            return permisosRolService.TienePermiso(idRol, PermisosSistema.usuariosGestionar);
        }

        private static Auditorias Auditoria(string accion, int idActor, string? antes, string? despues)
        {
            return new Auditorias
            {
                tabla = tablaAuditoria,
                accion = accion,
                idUsuario = idActor,
                fecha = DateTime.Now,
                datosAnteriores = antes,
                datosNuevos = despues
            };
        }

        private IActionResult Prohibido(string motivo)
        {
            return StatusCode(403, new { statusCode = 403, message = motivo });
        }

        private IActionResult SinSesion()
        {
            return Unauthorized(new { statusCode = 401, message = "La sesión no es válida." });
        }

        private IActionResult IdInvalido()
        {
            return BadRequest(new { statusCode = 400, message = "El ID debe ser mayor que cero." });
        }

        private IActionResult NoEncontrado()
        {
            return NotFound(new { statusCode = 404, message = "No se encontró el usuario." });
        }

        private IActionResult CorreoRepetido()
        {
            return Conflict(new { statusCode = 409, message = "Ya existe un usuario registrado con ese correo electrónico." });
        }

        private IActionResult DatosInvalidos()
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Los datos enviados no son válidos.",
                errors = new ValidationProblemDetails(ModelState).Errors
            });
        }

        private IActionResult ConflictoIntegridad(DbUpdateException ex)
        {
            logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

            return Conflict(new
            {
                statusCode = 409,
                message = "No fue posible guardar el usuario: el correo ya existe o el rol/tipo de documento indicado no es válido."
            });
        }

        private IActionResult ErrorInterno(Exception ex, string mensaje)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

            return StatusCode(500, new { statusCode = 500, message = mensaje });
        }
    }
}
