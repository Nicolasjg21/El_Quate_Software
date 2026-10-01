using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SQLConnectionStrings");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró la cadena de conexión 'SQLConnectionStrings'. " +
                    "Defínala mediante la variable de entorno ConnectionStrings__SQLConnectionStrings, user-secrets o un gestor de secretos.");
            }

            services.AddDbContext<ElQuateDePattyContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure();
                    }));

            services.AddMemoryCache();

            // Más iteraciones PBKDF2 que el valor por defecto; los hashes antiguos se
            // regeneran automáticamente en el siguiente inicio de sesión (SuccessRehashNeeded).
            services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);

            services.AddSingleton<IPasswordHasher<Usuarios>, PasswordHasher<Usuarios>>();
            services.AddSingleton<IRevocacionTokenService, RevocacionTokenService>();
            services.AddSingleton<IRastreadorIntentosLogin, RastreadorIntentosLogin>();
            services.AddScoped<IPermisosRolService, PermisosRolService>();
            services.AddSingleton<ITokenService, TokenService>();

            services.AddScoped<IAnaliticasRepository, AnaliticasRepository>();
            services.AddScoped<IAuditoriasRepository, AuditoriasRepository>();
            services.AddScoped<ICategoriasRepository, CategoriasRepository>();
            services.AddScoped<IComprasRepository, ComprasRepository>();
            services.AddScoped<IComprobantesRepository, ComprobantesRepository>();
            services.AddScoped<ICuentasRepository, CuentasRepository>();
            services.AddScoped<IDetalleComprasRepository, DetalleComprasRepository>();
            services.AddScoped<IDetallePedidosRepository, DetallePedidosRepository>();
            services.AddScoped<IKardexRepository, KardexRepository>();
            services.AddScoped<IMesasRepository, MesasRepository>();
            services.AddScoped<IMetodosPagoRepository, MetodosPagoRepository>();
            services.AddScoped<IPedidosRepository, PedidosRepository>();
            services.AddScoped<IPermisosRepository, PermisosRepository>();
            services.AddScoped<IProductosRepository, ProductosRepository>();
            services.AddScoped<IProveedoresRepository, ProveedoresRepository>();
            services.AddScoped<IRolesPermisosRepository, RolesPermisosRepository>();
            services.AddScoped<IRolesRepository, RolesRepository>();
            services.AddScoped<ITipoDocumentoRepository, TipoDocumentoRepository>();
            services.AddScoped<IUsuariosRepository, UsuariosRepository>();

            return services;
        }
    }
}
