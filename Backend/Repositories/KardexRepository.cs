using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class KardexRepository : IKardexRepository
    {
        private readonly ElQuateDePattyContext context;

        public KardexRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Kardex>> GetKardex()
        {
            var data = await context.kardex.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Kardex?> GetKardexById(int id)
        {
            var data = await context.kardex.FirstOrDefaultAsync(x => x.idMovimiento == id);
            return data;
        }

        public async Task<bool> PostKardex(Kardex kardex)
        {
            await context.kardex.AddAsync(kardex);
            return await context.BoolAsync();
        }

        /// <summary>
        /// Registra un movimiento de inventario con el stock calculado en el servidor.
        /// <list type="bullet">
        /// <item>stockAnterior: stockNuevo del último movimiento del producto (0 si no tiene).</item>
        /// <item>ENTRADA suma la cantidad, SALIDA la resta y AJUSTE fija el valor recibido en stockNuevo.</item>
        /// <item>La fecha es la del servidor; los valores de stock y fecha enviados por el cliente se ignoran.</item>
        /// </list>
        /// Un bloqueo de aplicación por producto (sp_getapplock) atiende por turnos los movimientos
        /// simultáneos del mismo producto, de modo que cada uno parte del stock confirmado por el anterior.
        /// </summary>
        public async Task<(bool registrado, int stock)> RegistrarMovimiento(Kardex kardex)
        {
            // Con EnableRetryOnFailure, una transacción explícita debe ejecutarse dentro de la
            // estrategia de reintentos: si la conexión falla, la operación completa se repite.
            var estrategia = context.Database.CreateExecutionStrategy();

            return await estrategia.ExecuteAsync<(bool registrado, int stock)>(async () =>
            {
                await using var transaccion = await context.Database.BeginTransactionAsync();

                // Turno exclusivo del producto hasta que la transacción termine (máximo 10 s de espera).
                var recurso = "stock-producto-" + kardex.idProducto;
                await context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @resultado int;
                    EXEC @resultado = sp_getapplock
                        @Resource = {recurso}, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = 10000;
                    IF @resultado < 0
                        THROW 50001, 'No fue posible reservar el stock del producto. Intente de nuevo.', 1;");

                // Stock vigente: último movimiento del producto, con el mismo orden que usa Productos/Filtrar.
                var anterior = await context.kardex
                    .AsNoTracking()
                    .Where(k => k.idProducto == kardex.idProducto)
                    .OrderByDescending(k => k.fecha)
                    .ThenByDescending(k => k.idMovimiento)
                    .Select(k => (int?)k.stockNuevo)
                    .FirstOrDefaultAsync() ?? 0;

                var nuevo = kardex.tipoMovimiento switch
                {
                    "ENTRADA" => anterior + kardex.cantidad,
                    "SALIDA" => anterior - kardex.cantidad,
                    _ => kardex.stockNuevo // AJUSTE: valor indicado por un usuario autorizado
                };

                if (nuevo < 0)
                {
                    await transaccion.RollbackAsync();
                    return (false, anterior);
                }

                kardex.stockAnterior = anterior;
                kardex.stockNuevo = nuevo;
                kardex.fecha = DateTime.Now;

                if (kardex.tipoMovimiento == "AJUSTE")
                {
                    kardex.cantidad = Math.Abs(nuevo - anterior);
                }

                context.kardex.Add(kardex);
                await context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return (true, nuevo);
            });
        }

        public async Task<bool> PutKardex(Kardex kardex)
        {
            context.kardex.Update(kardex);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteKardex(Kardex kardex)
        {
            context.kardex.Remove(kardex);
            return await context.BoolAsync();
        }
    }
}