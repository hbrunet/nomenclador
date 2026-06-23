using NHibernate;
using NHibernate.Linq;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class CatalogRepository(NHibernate.ISession session)
{
    public async Task<CatalogSnapshot> GetSnapshotAsync()
    {
        var nomencladores = await session.Query<NomencladorCatalogEntity>().ToListAsync();
        var escalas = await session.Query<EscalaSalarialCatalogEntity>().ToListAsync();
        var zonas = await session.Query<ZonaCatalogEntity>().ToListAsync();
        var categorias = await session.Query<CategoriaCatalogEntity>().ToListAsync();
        var conceptos = await session.Query<ConceptoCatalogEntity>().ToListAsync();
        var valoresFijos = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .ToListAsync();
        var valoresCategorias = await session.Query<ValorCategoriaCatalogEntity>()
            .Fetch(x => x.Tipo)
            .ToListAsync();

        return new CatalogSnapshot
        {
            Nomencladores = nomencladores.ToDictionary(item => item.Id),
            EscalasSalariales = escalas.ToDictionary(item => item.Id),
            Zonas = zonas.ToDictionary(item => item.Id),
            Categorias = categorias.ToDictionary(item => item.Id),
            Conceptos = conceptos.ToDictionary(item => item.Id),
            ValoresFijos = valoresFijos.ToDictionary(item => item.Id),
            ValoresCategorias = valoresCategorias.ToDictionary(item => item.Id)
        };
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetNomencladoresAsync()
    {
        return await session.Query<NomencladorCatalogEntity>()
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto { Id = item.Id, Descripcion = item.Descripcion })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetEscalasAsync()
    {
        return await session.Query<EscalaSalarialCatalogEntity>()
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto { Id = item.Id, Descripcion = item.Descripcion })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetZonasAsync()
    {
        return await session.Query<ZonaCatalogEntity>()
            .OrderBy(item => item.Descripcion)
            .Select(item => new CatalogItemDto { Id = item.Id, Descripcion = item.Descripcion })
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<CategoriaCatalogDto>> GetCategoriasAsync(int? escalaId)
    {
        var query = session.Query<CategoriaCatalogEntity>();

        if (escalaId.HasValue)
            query = query.Where(item => item.EscalaSalarialId == escalaId.Value);

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
        var items = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .OrderBy(item => item.Descripcion)
            .ToListAsync();

        return items.Select(item => new ValorFijoCatalogDto
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
            Tipo = item.Tipo?.Descripcion ?? string.Empty
        }).ToList();
    }
}

