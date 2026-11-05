using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Ayudas;

/// <summary>Utilidades compartidas por las pruebas de controladores.</summary>
public static class Respuestas
{
    /// <summary>Lee una propiedad del objeto anónimo { statusCode, message, data, ... } que devuelve la API.</summary>
    public static object? Leer(IActionResult resultado, string propiedad)
    {
        var valor = (resultado as ObjectResult)?.Value;
        return valor?.GetType().GetProperty(propiedad)?.GetValue(valor);
    }

    /// <summary>Contexto HTTP con un usuario autenticado (o anónimo si id es null).</summary>
    public static ControllerContext ContextoConUsuario(int? idUsuario, params Claim[] extras)
    {
        var claims = new List<Claim>(extras);
        if (idUsuario.HasValue) claims.Add(new Claim(ClaimTypes.NameIdentifier, idUsuario.Value.ToString()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, idUsuario.HasValue ? "Prueba" : null));
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }
}
