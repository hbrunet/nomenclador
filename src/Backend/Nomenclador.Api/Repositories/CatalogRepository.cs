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

        return items.Select(ToValorFijoCatalogDto).ToList();
    }

    public async Task<ValorFijoCatalogDto?> GetValorFijoByIdAsync(int id)
    {
        var entity = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity is null ? null : ToValorFijoCatalogDto(entity);
    }

    public async Task<ValorFijoUsagesDto> GetValorFijoUsagesAsync(int id)
    {
        var count = await session.Query<ValorFijoConfiguradoEntity>()
            .Where(x => x.ValorFijoId == id)
            .Select(x => x.ConfiguracionNomencladorId)
            .Distinct()
            .CountAsync();

        return new ValorFijoUsagesDto { Count = count };
    }

    public async Task<ValorFijoCatalogDto?> UpdateValorFijoAsync(int id, decimal valor)
    {
        var entity = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return null;

        entity.Valor = valor;

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToValorFijoCatalogDto(entity);
    }

    public async Task<ValorFijoCatalogDto> CreateValorFijoAsync(ValorFijoCreateDto dto)
    {
        var entity = new ValorFijoCatalogEntity
        {
            Descripcion = dto.Descripcion,
            Tipo = session.Load<ValorFijoTipoCatalogEntity>(dto.IdTipo),
            Valor = dto.Valor
        };

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await tx.CommitAsync();

        // Inicializar el proxy para obtener la descripción del tipo en la respuesta
        await NHibernateUtil.InitializeAsync(entity.Tipo);

        return ToValorFijoCatalogDto(entity);
    }

    private static ValorFijoCatalogDto ToValorFijoCatalogDto(ValorFijoCatalogEntity item) =>
        new()
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
            IdTipo = item.Tipo?.Id ?? 0,
            Tipo = item.Tipo?.Descripcion ?? string.Empty,
            Valor = item.Valor
        };

    public async Task<IReadOnlyCollection<ValorCategoriaCatalogDto>> GetValoresCategoriasAsync()
    {
        var items = await session.Query<ValorCategoriaCatalogEntity>()
            .Fetch(x => x.Tipo)
            .OrderBy(item => item.Descripcion)
            .ToListAsync();

        return items.Select(item => new ValorCategoriaCatalogDto
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
            Tipo = item.Tipo?.Descripcion ?? string.Empty
        }).ToList();
    }
}

