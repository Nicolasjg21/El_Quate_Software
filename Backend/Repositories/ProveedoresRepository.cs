using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ProveedoresRepository : IProveedoresRepository
    {
        private readonly ElQuateDePattyContext context;

        public ProveedoresRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Proveedores>> GetProveedores()
        {
            var data = await context.proveedores.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Proveedores?> GetProveedoresById(int id)
        {
            var data = await context.proveedores.FirstOrDefaultAsync(x => x.idProveedor == id);
            return data;
        }

        public async Task<bool> PostProveedores(Proveedores proveedores)
        {
            await context.proveedores.AddAsync(proveedores);
            return await context.BoolAsync();
        }

        public async Task<bool> PutProveedores(Proveedores proveedores)
        {
            context.proveedores.Update(proveedores);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteProveedores(Proveedores proveedores)
        {
            context.proveedores.Remove(proveedores);
            return await context.BoolAsync();
        }

        public async Task<List<Proveedores>> FiltrarProveedores(
    ProveedorFiltroDTO filtro)
        {
            var query = context.proveedores.AsNoTracking().AsQueryable();

            // -----------------------------------------
            // 1. FILTRO POR NOMBRE
            // -----------------------------------------
            if (!string.IsNullOrWhiteSpace(filtro.nombreProveedor))
            {
                query = query.Where(p =>
                    p.nombreProveedor.Contains(filtro.nombreProveedor));
            }

            // -----------------------------------------
            // 2. FILTRO POR TELÉFONO
            // -----------------------------------------
            if (!string.IsNullOrWhiteSpace(filtro.telefono))
            {
                query = query.Where(p =>
                    p.telefono.Contains(filtro.telefono));
            }

            // -----------------------------------------
            // 3. FILTRO POR DIRECCIÓN
            // -----------------------------------------
            if (!string.IsNullOrWhiteSpace(filtro.direccion))
            {
                query = query.Where(p =>
                    p.direccion.Contains(filtro.direccion));
            }

            return await query.ToListAsync();
        }
    }
}