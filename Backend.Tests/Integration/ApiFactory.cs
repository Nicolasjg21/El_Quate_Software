using ElQuateDePatty.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration;

/// <summary>
/// Levanta la API completa en memoria (TestServer) con una base EF InMemory.
/// Requiere la línea "public partial class Program { }" al final de Backend/Program.cs.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        // Program.cs se niega a arrancar sin estos tres valores. Son FALSOS, solo para pruebas.
        Environment.SetEnvironmentVariable("ConnectionStrings__SQLConnectionStrings", "Server=no-se-usa;Database=pruebas;");
        Environment.SetEnvironmentVariable("Jwt__Key", "clave-solo-para-pruebas-con-mas-de-32-bytes");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" y no "Development": en Development .NET cargaría tus user-secrets reales.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Quitar el registro de SQL Server (en EF Core 9 son dos piezas) y poner una base en memoria.
            var registros = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ElQuateDePattyContext>) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<ElQuateDePattyContext>)).ToList();
            foreach (var d in registros) services.Remove(d);

            services.AddDbContext<ElQuateDePattyContext>(o => o.UseInMemoryDatabase("ElQuatePruebas"));
        });
    }
}
