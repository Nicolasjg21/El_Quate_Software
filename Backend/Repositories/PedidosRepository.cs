using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.DTOs.MetodosPago;
using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class PedidosRepository : IPedidosRepository
    {
        private readonly ElQuateDePattyContext context;

        public PedidosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Pedidos>> GetPedidos()
        {
            var data = await context.pedidos.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Pedidos?> GetPedidosById(int id)
        {
            var data = await context.pedidos.FirstOrDefaultAsync(x => x.idPedido == id);
            return data;
        }

        public async Task<bool> PostPedidos(Pedidos pedidos)
        {
            await context.pedidos.AddAsync(pedidos);
            return await context.BoolAsync();
        }

        public async Task<bool> PutPedidos(Pedidos pedidos)
        {
            context.pedidos.Update(pedidos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeletePedidos(Pedidos pedidos)
        {
            context.pedidos.Remove(pedidos);
            return await context.BoolAsync();
        }

        public async Task<List<PedidoFiltradoRespuestaDTO>> FiltrarPedidos(PedidoFiltroDTO filtro)
        {
            var query = context.pedidos
                .AsNoTracking()
                .AsQueryable();

            // Mesa
            if (filtro.numeroMesa.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.mesa.numeroMesa == filtro.numeroMesa.Value);
            }

            // Usuario
            if (filtro.idUsuario.HasValue)
            {
                query = query.Where(p =>
                    p.idUsuario == filtro.idUsuario.Value);
            }

            // Fecha
            if (filtro.fecha.HasValue)
            {
                var fechaInicio = filtro.fecha.Value.Date;
                var fechaFin = fechaInicio.AddDays(1);

                query = query.Where(p =>
                    p.fecha >= fechaInicio && p.fecha < fechaFin);
            }

            // Hora Inicio
            if (filtro.horaInicio.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay >= filtro.horaInicio.Value);
            }

            // Hora Fin
            if (filtro.horaFin.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay <= filtro.horaFin.Value);
            }

            // Total mínimo
            if (filtro.totalMinimo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total >= filtro.totalMinimo.Value);
            }

            // Total máximo
            if (filtro.totalMaximo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total <= filtro.totalMaximo.Value);
            }

            // Método de pago
            if (filtro.idMetodoPago.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.comprobantes.Any(c =>
                        c.idMetodo == filtro.idMetodoPago.Value));
            }

            return await query
                .Select(p => new PedidoFiltradoRespuestaDTO
                {
                    idPedido = p.idPedido,
                    idCuenta = p.idCuenta,
                    idUsuario = p.idUsuario,
                    fecha = p.fecha,
                    estadoPedido = p.estadoPedido,
                    cuenta = new CuentaFiltradaDTO
                    {
                        idCuenta = p.cuenta.idCuenta,
                        idMesa = p.cuenta.idMesa,
                        estado = p.cuenta.estado,
                        fechaApertura = p.cuenta.fechaApertura,
                        fechaCierre = p.cuenta.fechaCierre,
                        total = p.cuenta.total,
                        mesa = new MesaRespuestaDTO
                        {
                            idMesa = p.cuenta.mesa.idMesa,
                            numeroMesa = p.cuenta.mesa.numeroMesa,
                            estado = p.cuenta.mesa.estado
                        },
                        comprobantes = p.cuenta.comprobantes
                            .Select(c => new ComprobanteFiltradoDTO
                            {
                                idComprobante = c.idComprobante,
                                idCuenta = c.idCuenta,
                                fecha = c.fecha,
                                total = c.total,
                                idMetodo = c.idMetodo,
                                metodoPago = new MetodoPagoRespuestaDTO
                                {
                                    idMetodo = c.metodoPago.idMetodo,
                                    nombreMetodo = c.metodoPago.nombreMetodo
                                }
                            })
                            .ToList()
                    },
                    usuario = new UsuarioRespuestaDTO
                    {
                        idUsuario = p.usuario.idUsuario,
                        nombres = p.usuario.nombres,
                        apellidos = p.usuario.apellidos,
                        documento = p.usuario.documento,
                        idTipoDocumento = p.usuario.idTipoDocumento,
                        telefono = p.usuario.telefono,
                        estado = p.usuario.estado,
                        idRol = p.usuario.idRol,
                        email = p.usuario.email
                    }
                })
                .ToListAsync();
        }

        public async Task<List<Pedidos>> FiltrarHistorialPedidos(PeriodoFiltroDTO filtro)
        {
            var query = context.pedidos.AsNoTracking().AsQueryable();

            if (filtro.fechaDesde.HasValue)
            {
                query = query.Where(p =>
                    p.fecha >= filtro.fechaDesde.Value);
            }

            if (filtro.fechaHasta.HasValue)
            {
                query = query.Where(p =>
                    p.fecha <= filtro.fechaHasta.Value);
            }

            return await query
                .OrderByDescending(p => p.fecha)
                .ToListAsync();
        }
    }
}
