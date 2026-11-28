using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ElQuateDePatty.Context;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class CuentasRepository : ICuentasRepository
    {
        private const string cuentaAbierta = "Abierta";
        private const string cuentaCerrada = "Cerrada";
        private const string pedidoPagado = "Pagado";
        private const string mesaLibre = "Libre";
        private const string mesaInactiva = "Inactiva";

        private static readonly JsonSerializerOptions opcionesAuditoria = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private readonly ElQuateDePattyContext context;

        public CuentasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Lista las cuentas. El filtro por estado se resuelve en la base de datos, de modo que
        /// solo viajan las filas solicitadas (por ejemplo, las cuentas abiertas que usa Mesas).
        /// </summary>
        public async Task<List<Cuentas>> GetCuentas(string? estado = null)
        {
            var consulta = context.cuentas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var filtro = estado.Trim();
                consulta = consulta.Where(c => c.estado == filtro);
            }

            return await consulta.ToListAsync();
        }

        public async Task<Cuentas?> GetCuentasById(int id)
        {
            var data = await context.cuentas.FirstOrDefaultAsync(x => x.idCuenta == id);
            return data;
        }

        public async Task<bool> PostCuentas(Cuentas cuentas)
        {
            await context.cuentas.AddAsync(cuentas);
            return await context.BoolAsync();
        }

        public async Task<bool> PutCuentas(Cuentas cuentas)
        {
            context.cuentas.Update(cuentas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCuentas(Cuentas cuentas)
        {
            context.cuentas.Remove(cuentas);
            return await context.BoolAsync();
        }

        /// <summary>
        /// Cobra una cuenta en una sola transacción del servidor. Todo o nada:
        /// <list type="number">
        /// <item>Reserva la cuenta (sp_getapplock "cobro-cuenta-{id}"): dos cobros simultáneos de la misma
        /// cuenta se atienden por turnos y el segundo encuentra el comprobante del primero.</item>
        /// <item>Valida: la cuenta existe, no tiene comprobante, está Abierta, el método de pago existe,
        /// tiene productos y su total coincide con el que vio quien cobra (si lo envió).</item>
        /// <item>El total sale de las líneas de la cuenta (cantidad × precio de carta), no del cliente.
        /// La base y el INC se separan con ImpuestoConsumo.</item>
        /// <item>Emite el comprobante, cierra la cuenta con la fecha del servidor, marca los pedidos como
        /// Pagado, libera la mesa (salvo que esté Inactiva) y escribe la auditoría con el usuario del token.</item>
        /// </list>
        /// Si algo falla, la transacción se deshace y no queda nada a medias. El índice único
        /// UX_Comprobantes_idCuenta es la última barrera contra un comprobante duplicado.
        /// </summary>
        public async Task<ResultadoCobro> Cobrar(int idCuenta, int idMetodo, decimal? totalEsperado, int idUsuario)
        {
            // Con EnableRetryOnFailure, la transacción explícita debe ir dentro de la estrategia de reintentos.
            var estrategia = context.Database.CreateExecutionStrategy();

            return await estrategia.ExecuteAsync<ResultadoCobro>(async () =>
            {
                // Si la estrategia repite la operación, se parte de cero, sin cambios del intento fallido.
                context.ChangeTracker.Clear();

                // La base en memoria de las pruebas no maneja transacciones ni sp_getapplock.
                var relacional = context.Database.IsRelational();
                await using var transaccion = relacional ? await context.Database.BeginTransactionAsync() : null;

                if (relacional)
                {
                    // Turno exclusivo de la cuenta hasta que la transacción termine (máximo 10 s de espera).
                    var recurso = "cobro-cuenta-" + idCuenta;
                    await context.Database.ExecuteSqlInterpolatedAsync($@"
                        DECLARE @resultado int;
                        EXEC @resultado = sp_getapplock
                            @Resource = {recurso}, @LockMode = 'Exclusive',
                            @LockOwner = 'Transaction', @LockTimeout = 10000;
                        IF @resultado < 0
                            THROW 50001, 'La cuenta se está cobrando en otro equipo. Intente de nuevo.', 1;");
                }

                var cuenta = await context.cuentas
                    .Include(c => c.mesa)
                    .Include(c => c.comprobantes)
                    .Include(c => c.pedidos).ThenInclude(p => p.detalles)
                    .FirstOrDefaultAsync(c => c.idCuenta == idCuenta);

                // Al salir sin confirmar, la transacción se deshace sola (no se escribió nada).
                if (cuenta == null)
                    return new ResultadoCobro(EstadoCobro.CuentaNoExiste);

                var existente = cuenta.comprobantes.OrderBy(c => c.idComprobante).FirstOrDefault();
                if (existente != null)
                    return new ResultadoCobro(EstadoCobro.YaCobrada, cuenta, existente, existente.total);

                if (!string.Equals(cuenta.estado?.Trim(), cuentaAbierta, StringComparison.OrdinalIgnoreCase))
                    return new ResultadoCobro(EstadoCobro.CuentaNoAbierta, cuenta);

                if (!await context.metodosPago.AnyAsync(m => m.idMetodo == idMetodo))
                    return new ResultadoCobro(EstadoCobro.MetodoNoExiste, cuenta);

                var total = cuenta.pedidos.SelectMany(p => p.detalles).Sum(d => d.cantidad * d.precioUnitario);

                if (total <= 0)
                    return new ResultadoCobro(EstadoCobro.SinProductos, cuenta, null, total);

                if (totalEsperado.HasValue && totalEsperado.Value != total)
                    return new ResultadoCobro(EstadoCobro.TotalDesactualizado, cuenta, null, total);

                var ahora = DateTime.Now;
                var totalAnterior = cuenta.total;
                var estadoCuentaAnterior = cuenta.estado;
                var mesa = cuenta.mesa;
                var numeroMesa = mesa?.numeroMesa ?? 0;
                var estadoMesaAnterior = mesa?.estado;

                var comprobante = new Comprobantes
                {
                    idCuenta = cuenta.idCuenta,
                    fecha = ahora,
                    total = total,
                    idMetodo = idMetodo
                };
                comprobante.AplicarImpuesto();
                context.comprobantes.Add(comprobante);

                cuenta.estado = cuentaCerrada;
                cuenta.fechaCierre = ahora;
                cuenta.total = total;

                foreach (var pedido in cuenta.pedidos)
                    pedido.estadoPedido = pedidoPagado;

                var liberarMesa = mesa != null
                    && !string.Equals(mesa.estado?.Trim(), mesaInactiva, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(mesa.estado?.Trim(), mesaLibre, StringComparison.OrdinalIgnoreCase);

                if (liberarMesa)
                    mesa!.estado = mesaLibre;

                // Primer guardado dentro de la transacción: la base asigna el número del comprobante.
                await context.SaveChangesAsync();

                context.auditorias.Add(Auditoria("Comprobantes", "INSERT", idUsuario, ahora, null, new
                {
                    comprobante.idComprobante, comprobante.idCuenta, numeroMesa, comprobante.idMetodo,
                    comprobante.total, comprobante.baseGravable, comprobante.impuesto
                }));

                context.auditorias.Add(Auditoria("Cuentas", "UPDATE", idUsuario, ahora,
                    new { cuenta.idCuenta, numeroMesa, estado = estadoCuentaAnterior, total = totalAnterior },
                    new { cuenta.idCuenta, numeroMesa, cuenta.estado, cuenta.fechaCierre, cuenta.total }));

                if (liberarMesa)
                    context.auditorias.Add(Auditoria("Mesas", "UPDATE", idUsuario, ahora,
                        new { numeroMesa, estado = estadoMesaAnterior },
                        new { numeroMesa, estado = mesaLibre }));

                await context.SaveChangesAsync();

                if (transaccion != null)
                    await transaccion.CommitAsync();

                return new ResultadoCobro(EstadoCobro.Cobrada, cuenta, comprobante, total);
            });
        }

        private static Auditorias Auditoria(string tabla, string accion, int idUsuario, DateTime fecha, object? antes, object? despues)
        {
            return new Auditorias
            {
                tabla = tabla,
                accion = accion,
                idUsuario = idUsuario,
                fecha = fecha,
                datosAnteriores = antes == null ? null : JsonSerializer.Serialize(antes, opcionesAuditoria),
                datosNuevos = despues == null ? null : JsonSerializer.Serialize(despues, opcionesAuditoria)
            };
        }
    }
}