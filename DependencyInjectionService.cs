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
            string connectionString = "";
            connectionString = _configuration["ConnectionStrings:SQLConnectionStrings"];

            services.AddDbContext<ElQuateDePattyContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IUsuariosRepository, UsuariosRepository>();

            return services;
        }
    }
}