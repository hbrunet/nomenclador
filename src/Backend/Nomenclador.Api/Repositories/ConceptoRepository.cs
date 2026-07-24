using NHibernate;
using NHibernate.Linq;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class ConceptoRepository(NHibernate.ISession session)
{
    public async Task<IReadOnlyCollection<ConceptoCatalogDto>> GetAllAsync(string? query)
    {
        var concepts = session.Query<ConceptoCatalogEntity>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            concepts = concepts.Where(item =>
                item.Codigo.Contains(q) ||
                item.DescripcionBreve.ToLower().Contains(q) ||
                item.Descripcion.ToLower().Contains(q));
        }

        return await concepts
            .OrderBy(item => item.Codigo)
            // Tope de seguridad: el catálogo de conceptos es grande, nunca devolvemos
            // más de 100 resultados aunque el término de búsqueda sea muy genérico.
            .Take(100)
            .Select(item => new ConceptoCatalogDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Subcodigo = item.Subcodigo,
                DescripcionBreve = item.DescripcionBreve,
                Descripcion = item.Descripcion,
            })
            .ToListAsync();
    }
}

