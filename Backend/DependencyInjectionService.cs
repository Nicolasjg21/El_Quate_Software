using ElQuateDePatty.Context;
using ElQuateDePatty.Repositories;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
namespace ElQuateDePatty 
{ 
    public static class DependencyInjectionService 
    { 
        public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration _configuration) 
        {
            string connectionString =
             _configuration.GetConnectionString("SQLConnectionStrings")
             ?? throw new InvalidOperationException(
                 "No se encontró la cadena de conexión 'SQLConnectionStrings'.");

            services.AddDbContext<ElQuateDePattyContext>(options => 
                options.UseSqlServer(
                    connectionString, 
                    sqlOptions => 
                    { 
                        sqlOptions.EnableRetryOnFailure(); 
                    }

        )); 
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