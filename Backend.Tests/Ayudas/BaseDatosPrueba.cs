using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Ayudas;

/// <summary>Crea un contexto con una base EF Core en memoria nueva y aislada (un nombre único por prueba).</summary>
public static class BaseDatosPrueba
{
    public static ElQuateDePattyContext Crear() =>
        new(new DbContextOptionsBuilder<ElQuateDePattyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
