using ElQuateDePatty;
using ElQuateDePatty.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using ElQuateDePatty.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 1_048_576;
});

builder.Services.AddExternal(builder.Configuration);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Respuesta de validación con la misma forma { statusCode, message } que el resto de la API.
        options.InvalidModelStateResponseFactory = contexto =>
        {
            var detalle = new ValidationProblemDetails(contexto.ModelState);

            return new BadRequestObjectResult(new
            {
                statusCode = 400,
                message = "Los datos enviados no son válidos.",
                errors = detalle.Errors
            });
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepcionesGlobal>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ElQuateDePatty",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token JWT en el encabezado Authorization. Ejemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// -------------------------------------------------------------
// CORS: orígenes permitidos desde configuración (Cors:AllowedOrigins)
// -------------------------------------------------------------
// En Development sin configuración usa los orígenes locales; en otros entornos exige configurarlos.
var origenesPermitidos = OrigenesCors.Resolver(
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>(),
    builder.Environment.IsDevelopment());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(origenesPermitidos)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// -------------------------------------------------------------
// JWT
// -------------------------------------------------------------
var jwtSettings = new JwtSettings
{
    key = builder.Configuration["Jwt:Key"] ?? string.Empty,
    issuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty,
    audience = builder.Configuration["Jwt:Audience"] ?? string.Empty,
    expirationMinutes = builder.Configuration.GetValue("Jwt:ExpirationMinutes", 30)
};

jwtSettings.Validar();

builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    x.SaveToken = false;

    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ClockSkew = TimeSpan.FromMinutes(1),

        ValidIssuer = jwtSettings.issuer,
        ValidAudience = jwtSettings.audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.key)),

        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    // Revocación inmediata: si el usuario fue desactivado, eliminado o cambió de rol,
    // el token emitido deja de ser válido sin esperar a su expiración.
    x.Events = new JwtBearerEvents
    {
        OnTokenValidated = async contexto =>
        {
            var idUsuario = contexto.Principal?.ObtenerIdUsuario();
            var rolToken = contexto.Principal?.FindFirst(ClaimTypes.Role)?.Value;

            if (idUsuario == null)
            {
                contexto.Fail("Token sin identificador de usuario.");
                return;
            }

            var repositorio = contexto.HttpContext.RequestServices
                .GetRequiredService<IUsuariosRepository>();

            var usuario = await repositorio.ObtenerUsuarioActivoParaSesion(idUsuario.Value);

            if (usuario == null || usuario.idRol.ToString() != rolToken)
            {
                contexto.Fail("El usuario está inactivo o su rol cambió.");
                return;
            }

            // El sello deriva del hash de la contraseña: cambiarla invalida los tokens anteriores.
            var selloToken = contexto.Principal?.FindFirst(SelloSeguridad.claimSello)?.Value;

            if (selloToken != SelloSeguridad.Calcular(usuario.passwordHash))
            {
                contexto.Fail("La sesión ya no es válida (credenciales modificadas).");
                return;
            }

            // Logout: la sesión fue revocada explícitamente.
            var idSesion = contexto.Principal?.FindFirst(SelloSeguridad.claimSesion)?.Value;

            if (string.IsNullOrEmpty(idSesion) ||
                contexto.HttpContext.RequestServices
                    .GetRequiredService<IRevocacionTokenService>().EstaRevocada(idSesion))
            {
                contexto.Fail("La sesión fue cerrada.");
            }
        }
    };
});

// -------------------------------------------------------------
// Autorización
// -------------------------------------------------------------
var rolesAdministrador = builder.Configuration
    .GetSection("Authorization:AdminRoleIds")
    .Get<string[]>()?
    .Where(r => !string.IsNullOrWhiteSpace(r))
    .ToHashSet() ?? new HashSet<string>();

builder.Services.AddSingleton(new AutorizacionSettings { rolesAdministrador = rolesAdministrador });

// Las políticas "permiso:{nombre}" se resuelven contra Roles → RolesPermisos → Permisos.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// -------------------------------------------------------------
// Proxy inverso (opcional): solo se confía en X-Forwarded-* si se habilita por configuración
// -------------------------------------------------------------
var confiarEnProxy = builder.Configuration.GetValue("ForwardedHeaders:Habilitado", false);

if (confiarEnProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// -------------------------------------------------------------
// Límite de intentos en el login (por IP)
// -------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (confiarEnProxy)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-store";

    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    }

    await next();
});

app.UseCors("AllowSpecificOrigin");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();


// -------------------------------------------------------------
// Reanudación de la base de datos en segundo plano: Azure SQL sin servidor se pausa por
// inactividad. El servidor abre de inmediato (Swagger disponible) y, en paralelo, despierta
// la base para que esté lista antes del primer inicio de sesión. Nunca detiene la aplicación.
// -------------------------------------------------------------
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var alcance = app.Services.CreateScope();
        var contexto = alcance.ServiceProvider
            .GetRequiredService<ElQuateDePatty.Context.ElQuateDePattyContext>();
        var limite = DateTime.Now.AddMinutes(3);

        app.Logger.LogInformation("Reanudando la base de datos en segundo plano...");

        while (DateTime.Now < limite)
        {
            if (await contexto.Database.CanConnectAsync())
            {
                app.Logger.LogInformation("Base de datos disponible.");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        app.Logger.LogWarning("La base de datos no respondió en 3 minutos. Verifique su estado en Azure.");
    });
});

await app.RunAsync();

public partial class Program { }