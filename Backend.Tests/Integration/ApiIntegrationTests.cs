using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>CI-01 a CI-03 · Pipeline real (middlewares, JWT, autorización) contra la API en memoria.</summary>
public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient client;

    public ApiIntegrationTests(ApiFactory factory) => client = factory.CreateClient();

    // CI-01 · Humo: la API está viva.
    [Fact]
    public async Task Health_SinToken_Retorna200()
    {
        var respuesta = await client.GetAsync("/health");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // CI-02 · Seguridad: sin token no se entra.
    [Fact]
    public async Task GetProveedores_SinToken_Retorna401()
    {
        var respuesta = await client.GetAsync("/api/Proveedores/GetProveedores");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // CI-03 · Seguridad: correo inexistente → 401 genérico, sin revelar si existe.
    [Fact]
    public async Task Login_CorreoInexistente_Retorna401()
    {
        var respuesta = await client.PostAsJsonAsync("/api/Autenticador/Login",
            new { email = $"nadie.{Guid.NewGuid():N}@elcuate.com", password = "Cualquiera123" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
