using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.DTOs.Mesas;

namespace ElQuateDePatty.DTOs.Cuentas
{
    public class CuentaFiltradaDTO : CuentaRespuestaDTO
    {
        public MesaRespuestaDTO? mesa { get; set; }

        public List<ComprobanteFiltradoDTO> comprobantes { get; set; } = new();
    }
}
