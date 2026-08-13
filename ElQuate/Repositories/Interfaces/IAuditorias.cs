using ElQuateSoftware.Models; 

namespace ElQuateSoftwareSoftware.Repositories.Interfaces // Nombre del espacio, sin mucho detalle
{
    public interface IAuditoriasRepository // Nombre de la interfaz
    {
            Task<List<Auditorias>> GetAuditorias(); // Aqui decimos que traiga todos los datos de la db en una lista para ingresarlos en el modelo, o practiamente para eso es

            Task<bool> PostAuditorias(Auditorias auditorias); // conformar si el objero auditorias, el cual trae toda la info, logro pasar tos bien a Auditorias o no 
    }
}
