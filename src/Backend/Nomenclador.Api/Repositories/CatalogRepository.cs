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
                Numero = item.Numero,
                Monto = item.Monto
            })
            .ToListAsync();
    }

    public async Task UpdateCategoriaMontosAsync(IReadOnlyCollection<CategoriaMontoUpdateDto> items)
    {
        foreach (var item in items)
        {
            var entity = await session.GetAsync<CategoriaCatalogEntity>(item.Id);
            if (entity is not null)
                entity.Monto = item.Monto;
        }

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
    }

    // ── Escalas ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<EscalaListItemDto>> GetAllEscalasAsync()
    {
        var escalas = await session.Query<EscalaSalarialCatalogEntity>()
            .OrderBy(x => x.Descripcion)
            .ToListAsync();

        var categoriaEscalaIds = await session.Query<CategoriaCatalogEntity>()
            .Select(x => x.EscalaSalarialId)
            .ToListAsync();

        var countByEscala = categoriaEscalaIds
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return escalas.Select(e => new EscalaListItemDto
        {
            Id = e.Id,
            Descripcion = e.Descripcion,
            CantidadCategorias = countByEscala.GetValueOrDefault(e.Id, 0),
        }).ToList();
    }

    public async Task<EscalaDetailDto?> GetEscalaDetailAsync(int id)
    {
        var escala = await session.GetAsync<EscalaSalarialCatalogEntity>(id);
        if (escala is null) return null;

        var categorias = await session.Query<CategoriaCatalogEntity>()
            .Where(x => x.EscalaSalarialId == id)
            .OrderBy(x => x.Numero)
            .ToListAsync();

        return new EscalaDetailDto
        {
            Id = escala.Id,
            Descripcion = escala.Descripcion,
            Categorias = categorias.Select(ToCategoriaCatalogDto).ToList(),
        };
    }

    public async Task<EscalaDetailDto> CreateEscalaAsync(EscalaCreateUpdateDto dto)
    {
        var entity = new EscalaSalarialCatalogEntity { Descripcion = dto.Descripcion };

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        return new EscalaDetailDto { Id = entity.Id, Descripcion = entity.Descripcion };
    }

    public async Task<EscalaDetailDto?> UpdateEscalaAsync(int id, EscalaCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<EscalaSalarialCatalogEntity>(id);
        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return await GetEscalaDetailAsync(id);
    }

    public async Task<bool> DeleteEscalaAsync(int id)
    {
        var inUse = await session.Query<ConfiguracionNomencladorEntity>()
            .Where(x => x.EscalaSalarialId == id)
            .CountAsync() > 0;

        if (inUse) return false;

        var categorias = await session.Query<CategoriaCatalogEntity>()
            .Where(x => x.EscalaSalarialId == id)
            .ToListAsync();

        var entity = await session.GetAsync<EscalaSalarialCatalogEntity>(id);
        if (entity is null) return true;

        using var tx = session.BeginTransaction();
        foreach (var cat in categorias)
            await session.DeleteAsync(cat);
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        return true;
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    public async Task<CategoriaCatalogDto> CreateCategoriaAsync(int escalaId, CategoriaCreateUpdateDto dto)
    {
        var entity = new CategoriaCatalogEntity
        {
            EscalaSalarialId = escalaId,
            Numero = dto.Numero,
            Descripcion = dto.Descripcion,
            Monto = dto.Monto,
        };

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToCategoriaCatalogDto(entity);
    }

    public async Task<CategoriaCatalogDto?> UpdateCategoriaAsync(int id, CategoriaCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<CategoriaCatalogEntity>(id);
        if (entity is null) return null;

        entity.Numero = dto.Numero;
        entity.Descripcion = dto.Descripcion;
        entity.Monto = dto.Monto;

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToCategoriaCatalogDto(entity);
    }

    public async Task DeleteCategoriaAsync(int id)
    {
        var entity = await session.GetAsync<CategoriaCatalogEntity>(id);
        if (entity is null) return;

        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
    }

    private static CategoriaCatalogDto ToCategoriaCatalogDto(CategoriaCatalogEntity c) =>
        new()
        {
            Id = c.Id,
            Descripcion = c.Descripcion,
            EscalaSalarialId = c.EscalaSalarialId,
            Numero = c.Numero,
            Monto = c.Monto,
        };

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

    public async Task<ValorFijoCatalogDto?> UpdateValorFijoAsync(int id, ValorFijoUpdateDto dto)
    {
        var entity = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;
        entity.Valor = dto.Valor;

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

        if (dto.ConfiguracionId.HasValue)
        {
            var asociacion = new ValorFijoConfiguradoEntity
            {
                ConfiguracionNomencladorId = dto.ConfiguracionId.Value,
                ValorFijoId = entity.Id,
            };
            await session.SaveAsync(asociacion);
        }

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

    public async Task<IReadOnlyCollection<ValorCategoriaConfiguradoItemDto>> UpdateValorCategoriaItemsAsync(
        int valorCategoriaId,
        IReadOnlyCollection<ValorCategoriaConfiguradoItemDto> items)
    {
        var existing = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Where(x => x.ValorCategoriaId == valorCategoriaId)
            .ToListAsync();

        using var tx = session.BeginTransaction();

        foreach (var item in existing)
            await session.DeleteAsync(item);

        var saved = new List<ValorCategoriaConfiguradoItemEntity>();
        foreach (var dto in items)
        {
            var entity = new ValorCategoriaConfiguradoItemEntity
            {
                ValorCategoriaId = valorCategoriaId,
                Numero = dto.NumeroCategoria,
                Importe = dto.Importe,
            };
            await session.SaveAsync(entity);
            saved.Add(entity);
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return saved
            .OrderBy(x => x.Numero)
            .Select(x => new ValorCategoriaConfiguradoItemDto
            {
                Id = x.Id,
                NumeroCategoria = x.Numero,
                Importe = x.Importe,
            })
            .ToList();
    }

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

    public async Task<IReadOnlyCollection<ValorCategoriaConfiguradoItemDto>?> GetValorCategoriaConfiguradoItemsAsync(int id)
    {
        var items = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Where(x => x.ValorCategoriaId == id)
            .OrderBy(x => x.Numero)
            .ToListAsync();

        return items.Select(item => new ValorCategoriaConfiguradoItemDto
        {
            Id = item.Id,
            NumeroCategoria = item.Numero,
            Importe = item.Importe,
        }).ToList();
    }
}

