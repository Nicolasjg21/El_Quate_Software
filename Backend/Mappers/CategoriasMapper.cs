using ElQuateDePatty.DTOs.Categorias;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class CategoriasMapper
    {
        public static Categorias ToEntity(this CategoriaCrearDTO dto)
        {
            return new Categorias
            {
                nombreCategoria = dto.nombreCategoria
            };
        }

        public static CategoriaRespuestaDTO ToRespuestaDTO(this Categorias entidad)
        {
            return new CategoriaRespuestaDTO
            {
                idCategoria = entidad.idCategoria,
                nombreCategoria = entidad.nombreCategoria
            };
        }

        public static List<CategoriaRespuestaDTO> ToRespuestaDTO(this IEnumerable<Categorias> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
