using Microsoft.AspNetCore.Diagnostics;

namespace ElQuateDePatty.Services
{
    public sealed class ManejadorExcepcionesGlobal : IExceptionHandler
    {
        private readonly ILogger<ManejadorExcepcionesGlobal> logger;

        public ManejadorExcepcionesGlobal(ILogger<ManejadorExcepcionesGlobal> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                return true;
            }

            logger.LogError(exception, "Excepción no controlada en {Metodo} {Ruta}",
                httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(new
            {
                statusCode = 500,
                message = "Ocurrió un error interno en el servidor."
            }, cancellationToken);

            return true;
        }
    }
}
