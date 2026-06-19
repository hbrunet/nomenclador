using Microsoft.EntityFrameworkCore;
using Nomenclador.Api.Data;
using Nomenclador.Api.DTOs;

namespace Nomenclador.Api.Repositories;

public sealed class ConceptoRepository(NomencladorDbContext dbContext)
{
    public async Task<IReadOnlyCollection<ConceptoCatalogDto>> GetAllAsync(string? query)
    {
        var concepts = dbContext.Conceptos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLowerInvariant();
            concepts = concepts.Where(item =>
                item.Codigo.Contains(normalizedQuery) ||
                item.DescripcionBreve.ToLower().Contains(normalizedQuery) ||
                item.Descripcion.ToLower().Contains(normalizedQuery) ||
                item.Clasificacion.ToLower().Contains(normalizedQuery));
        }

        return await concepts
            .OrderBy(item => item.Codigo)
            .Select(item => new ConceptoCatalogDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Subcodigo = item.Subcodigo,
                DescripcionBreve = item.DescripcionBreve,
                Descripcion = item.Descripcion,
                Clasificacion = item.Clasificacion
            })
            .ToListAsync();
    }
}
