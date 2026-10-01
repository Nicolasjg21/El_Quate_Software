using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.DTOs.Usuarios;

namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoFiltradoRespuestaDTO : PedidoRespuestaDTO
    {
        public CuentaFiltradaDTO? cuenta { get; set; }

        public UsuarioRespuestaDTO? usuario { get; set; }
    }
}
