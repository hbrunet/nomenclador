using Microsoft.EntityFrameworkCore;
using Nomenclador.Api.Data;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class CatalogRepository(NomencladorDbContext dbContext)
{
    public async Task<CatalogSnapshot> GetSnapshotAsync()
    {
        return new CatalogSnapshot
        {
            Nomencladores = await dbContext.Nomencladores.ToDictionaryAsync(item => item.Id),
            EscalasSalariales = await dbContext.EscalasSalariales.ToDictionaryAsync(item => item.Id),
            Zonas = await dbContext.Zonas.ToDictionaryAsync(item => item.Id),
            Categorias = await dbContext.Categorias.ToDictionaryAsync(item => item.Id),
            Conceptos = await dbContext.Conceptos.ToDictionaryAsync(item => item.Id),
            ValoresFijos = await dbContext.ValoresFijos.ToDictionaryAsync(item => item.Id)
        };
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetNomencladoresAsync()
    {
        return await dbContext.Nomencladores
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto
            {
                Id = item.Id,
                Descripcion = item.Descripcion
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetEscalasAsync()
    {
        return await dbContext.EscalasSalariales
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto
            {
                Id = item.Id,
                Descripcion = item.Descripcion
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetZonasAsync()
    {
        return await dbContext.Zonas
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto
            {
                Id = item.Id,
                Descripcion = item.Descripcion
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CategoriaCatalogDto>> GetCategoriasAsync(int? escalaId)
    {
        var query = dbContext.Categorias.AsQueryable();

        if (escalaId.HasValue)
        {
            query = query.Where(item => item.EscalaSalarialId == escalaId.Value);
        }

        return await query
            .OrderBy(item => item.Numero)
            .Select(item => new CategoriaCatalogDto
            {
                Id = item.Id,
                Descripcion = item.Descripcion,
                EscalaSalarialId = item.EscalaSalarialId,
                Numero = item.Numero
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ValorFijoCatalogDto>> GetValoresFijosAsync()
    {
        return await dbContext.ValoresFijos
            .OrderBy(item => item.Descripcion)
            .Select(item => new ValorFijoCatalogDto
            {
                Id = item.Id,
                Descripcion = item.Descripcion,
                Tipo = item.Tipo
            })
            .ToListAsync();
    }
}
