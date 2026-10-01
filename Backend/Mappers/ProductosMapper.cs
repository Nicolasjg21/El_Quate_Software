using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class ProductosMapper
    {
        public static Productos ToEntity(this ProductoCrearDTO dto)
        {
            return new Productos
            {
                nombreProducto = dto.nombreProducto,
                precioVenta = dto.precioVenta,
                cantidadMinima = dto.cantidadMinima,
                estado = dto.estado,
                idCategoria = dto.idCategoria
            };
        }

        public static ProductoDetalleRespuestaDTO ToRespuestaDTO(this Productos entidad)
        {
            return new ProductoDetalleRespuestaDTO
            {
                idProducto = entidad.idProducto,
                nombreProducto = entidad.nombreProducto,
                precioVenta = entidad.precioVenta,
                cantidadMinima = entidad.cantidadMinima,
                estado = entidad.estado,
                idCategoria = entidad.idCategoria
            };
        }

        public static List<ProductoDetalleRespuestaDTO> ToRespuestaDTO(this IEnumerable<Productos> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
