using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Linq;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class CatalogRepository(NHibernate.ISession session)
{
    // HACK legacy: ValorFijoCatalogEntity no tiene columna Periodo (agregarla arriesga
    // romper la app legacy que comparte la tabla), así que el período viaja como texto
    // libre dentro de Descripcion, indistintamente en formato "MM/YYYY" o "YYYY/MM"
    // según cómo se haya cargado originalmente; acá se reemplaza cualquier ocurrencia del
    // formato detectado por el nuevo período, preservando ese mismo formato. Si la
    // descripción no tiene ningún período reconocible, se lo agrega como sufijo.
    private static readonly Regex PeriodoMmYyyyRegex = new(@"\b\d{2}/\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex PeriodoYyyyMmRegex = new(@"\b\d{4}/\d{2}\b", RegexOptions.Compiled);

    private static string ReemplazarPeriodoEnDescripcion(string descripcion, DateOnly nuevoPeriodo)
    {
        if (PeriodoMmYyyyRegex.IsMatch(descripcion))
            return PeriodoMmYyyyRegex.Replace(descripcion, nuevoPeriodo.ToString("MM/yyyy"));

        if (PeriodoYyyyMmRegex.IsMatch(descripcion))
            return PeriodoYyyyMmRegex.Replace(descripcion, nuevoPeriodo.ToString("MM/yyyy"));

        return $"{descripcion.TrimEnd()} {nuevoPeriodo:MM/yyyy}";
    }

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

    /// <summary>
    /// Snapshot liviano para el listado paginado: ToListItemDto solo resuelve
    /// Nomenclador/Escala/Zona, así que evitamos traer Conceptos/Categorias/ValoresFijos/
    /// ValoresCategorias completos (catálogos grandes) sin necesidad.
    /// </summary>
    public async Task<CatalogSnapshot> GetSnapshotForListAsync()
    {
        var nomencladores = await session.Query<NomencladorCatalogEntity>().ToListAsync();
        var escalas = await session.Query<EscalaSalarialCatalogEntity>().ToListAsync();
        var zonas = await session.Query<ZonaCatalogEntity>().ToListAsync();

        return new CatalogSnapshot
        {
            Nomencladores = nomencladores.ToDictionary(item => item.Id),
            EscalasSalariales = escalas.ToDictionary(item => item.Id),
            Zonas = zonas.ToDictionary(item => item.Id),
            Categorias = new Dictionary<int, CategoriaCatalogEntity>(),
            Conceptos = new Dictionary<int, ConceptoCatalogEntity>(),
            ValoresFijos = new Dictionary<int, ValorFijoCatalogEntity>(),
            ValoresCategorias = new Dictionary<int, ValorCategoriaCatalogEntity>()
        };
    }

    /// <summary>
    /// Snapshot acotado a una única configuración: en vez de traer las tablas completas de
    /// Conceptos/Categorias/ValoresFijos/ValoresCategorias, filtra por los IDs que la entidad
    /// realmente referencia. El costo queda ligado al tamaño de la configuración, no al del catálogo.
    /// </summary>
    public async Task<CatalogSnapshot> GetSnapshotForEntityAsync(ConfiguracionNomencladorEntity entity)
    {
        var conceptoIds = entity.Conceptos.Select(item => item.ConceptoId).Distinct().ToList();
        var valorFijoIds = entity.ValoresFijos.Select(item => item.ValorFijoId).Distinct().ToList();
        var valorCategoriaIds = entity.ValoresCategorias.Select(item => item.ValorCategoriaId).Distinct().ToList();

        var nomenclador = await session.GetAsync<NomencladorCatalogEntity>(entity.NomencladorId);
        var escala = await session.GetAsync<EscalaSalarialCatalogEntity>(entity.EscalaSalarialId);
        var zona = entity.ZonaId.HasValue
            ? await session.GetAsync<ZonaCatalogEntity>(entity.ZonaId.Value)
            : null;

        var categorias = await session.Query<CategoriaCatalogEntity>()
            .Where(item => item.EscalaSalarialId == entity.EscalaSalarialId)
            .ToListAsync();

        var conceptos = conceptoIds.Count == 0
            ? []
            : await session.Query<ConceptoCatalogEntity>()
                .Where(item => conceptoIds.Contains(item.Id))
                .ToListAsync();

        var valoresFijos = valorFijoIds.Count == 0
            ? []
            : await session.Query<ValorFijoCatalogEntity>()
                .Fetch(x => x.Tipo)
                .Where(item => valorFijoIds.Contains(item.Id))
                .ToListAsync();

        var valoresCategorias = valorCategoriaIds.Count == 0
            ? []
            : await session.Query<ValorCategoriaCatalogEntity>()
                .Fetch(x => x.Tipo)
                .Where(item => valorCategoriaIds.Contains(item.Id))
                .ToListAsync();

        return new CatalogSnapshot
        {
            Nomencladores = nomenclador is null
                ? new Dictionary<int, NomencladorCatalogEntity>()
                : new Dictionary<int, NomencladorCatalogEntity> { [nomenclador.Id] = nomenclador },
            EscalasSalariales = escala is null
                ? new Dictionary<int, EscalaSalarialCatalogEntity>()
                : new Dictionary<int, EscalaSalarialCatalogEntity> { [escala.Id] = escala },
            Zonas = zona is null
                ? new Dictionary<int, ZonaCatalogEntity>()
                : new Dictionary<int, ZonaCatalogEntity> { [zona.Id] = zona },
            Categorias = categorias.ToDictionary(item => item.Id),
            Conceptos = conceptos.ToDictionary(item => item.Id),
            ValoresFijos = valoresFijos.ToDictionary(item => item.Id),
            ValoresCategorias = valoresCategorias.ToDictionary(item => item.Id)
        };
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetNomencladoresAsync()
    {
        return await session.Query<NomencladorCatalogEntity>()
            .OrderBy(item => item.Id)
            .ThenBy(item => item.Descripcion)
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

    // Clona cada escala salarial distinta (Descripcion con el período reemplazado, mismo
    // mecanismo que valores fijos/por categoría) junto con todas sus Categorias, aplicando
    // el coeficiente de ajuste al Monto de cada una. Usado por la actualización masiva de
    // escala salarial (clonar + reasignar a las configuraciones seleccionadas). Devuelve el
    // mapeo escala original → escala clonada para que el llamador reasigne cada configuración.
    public async Task<Dictionary<int, int>> CloneEscalasMasivoAsync(
        IReadOnlyCollection<int> escalaIds, DateOnly nuevoPeriodo, decimal coeficienteAjuste)
    {
        var ids = escalaIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        const int oracleInLimit = 1000;
        var escalas = new List<EscalaSalarialCatalogEntity>(ids.Count);
        var categorias = new List<CategoriaCatalogEntity>();

        for (var i = 0; i < ids.Count; i += oracleInLimit)
        {
            var batch = ids.Skip(i).Take(oracleInLimit).ToList();

            var batchEscalas = await session.Query<EscalaSalarialCatalogEntity>()
                .Where(x => batch.Contains(x.Id))
                .ToListAsync();
            escalas.AddRange(batchEscalas);

            var batchCategorias = await session.Query<CategoriaCatalogEntity>()
                .Where(x => batch.Contains(x.EscalaSalarialId))
                .ToListAsync();
            categorias.AddRange(batchCategorias);
        }

        var categoriasPorEscala = categorias
            .GroupBy(x => x.EscalaSalarialId)
            .ToDictionary(g => g.Key, g => g.ToList());

        using var tx = session.BeginTransaction();

        // Fase 1: clonar las escalas primero para obtener sus ids generados por secuencia,
        // necesarios como EscalaSalarialId de las categorías clonadas (fase 2).
        var clonesPorOriginal = new Dictionary<int, EscalaSalarialCatalogEntity>();
        foreach (var escala in escalas)
        {
            var clone = new EscalaSalarialCatalogEntity
            {
                Descripcion = ReemplazarPeriodoEnDescripcion(escala.Descripcion, nuevoPeriodo),
            };
            await session.SaveAsync(clone);
            clonesPorOriginal[escala.Id] = clone;
        }
        await session.FlushAsync();

        foreach (var (escalaOriginalId, clone) in clonesPorOriginal)
        {
            if (!categoriasPorEscala.TryGetValue(escalaOriginalId, out var categoriasOriginales)) continue;

            foreach (var categoria in categoriasOriginales)
            {
                await session.SaveAsync(new CategoriaCatalogEntity
                {
                    Descripcion = categoria.Descripcion,
                    EscalaSalarialId = clone.Id,
                    Numero = categoria.Numero,
                    Monto = Math.Round(categoria.Monto * coeficienteAjuste, 2, MidpointRounding.AwayFromZero),
                    DescLarga = categoria.DescLarga,
                });
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return clonesPorOriginal.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
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

        // Este endpoint solo actualiza el Valor. No tocar Descripcion aquí: el frontend
        // nunca la envía, y Oracle guarda un string vacío como NULL, lo que borraba
        // la descripción del catálogo (compartida por todas las configuraciones que
        // usan este valor fijo) en cada edición.
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
            Descripcion = item.Descripcion ?? string.Empty,
            IdTipo = item.Tipo?.Id ?? 0,
            Tipo = item.Tipo?.Descripcion ?? string.Empty,
            Valor = item.Valor
        };
        

    public async Task UpdateValorCategoriaItemsAsync(IReadOnlyCollection<ValorCategoriaConfiguradoItemDto> items)
    {
        foreach (var dto in items)
        {
            var entity = await session.GetAsync<ValorCategoriaConfiguradoItemEntity>(dto.Id);
            if (entity is not null)
                entity.Importe = dto.Importe;
        }

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
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
            IdTipo = item.Tipo?.Id ?? 0,
            Tipo = item.Tipo?.Descripcion ?? string.Empty
        }).ToList();
    }

    // ── Valores por Categoría ABM ─────────────────────────────────────────────

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetValorCategoriaTiposAsync()
    {
        return await session.Query<ValorCategoriaTipoCatalogEntity>()
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Descripcion)
            .Select(x => new CatalogItemDto { Id = x.Id, Descripcion = x.Descripcion })
            .ToListAsync();
    }

    public async Task<CatalogItemDto> CreateValorCategoriaTipoAsync(ValorCategoriaTipoCreateUpdateDto dto)
    {
        var entity = new ValorCategoriaTipoCatalogEntity { Descripcion = dto.Descripcion };
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return new CatalogItemDto { Id = entity.Id, Descripcion = entity.Descripcion };
    }

    public async Task<CatalogItemDto?> UpdateValorCategoriaTipoAsync(int id, ValorCategoriaTipoCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<ValorCategoriaTipoCatalogEntity>(id);
        if (entity is null) return null;
        entity.Descripcion = dto.Descripcion;
        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
        return new CatalogItemDto { Id = entity.Id, Descripcion = entity.Descripcion };
    }

    public async Task<bool> DeleteValorCategoriaTipoAsync(int id)
    {
        var inUse = await session.Query<ValorCategoriaCatalogEntity>()
            .Where(x => x.Tipo != null && x.Tipo.Id == id)
            .CountAsync() > 0;
        if (inUse) return false;

        var entity = await session.GetAsync<ValorCategoriaTipoCatalogEntity>(id);
        if (entity is null) return true;
        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<IReadOnlyCollection<ValorCategoriaListItemDto>> GetAllValoresCategoriasListAsync()
    {
        var valores = await session.Query<ValorCategoriaCatalogEntity>()
            .Fetch(x => x.Tipo)
            .OrderBy(x => x.Descripcion)
            .ToListAsync();

        var itemCounts = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Select(x => x.ValorCategoriaId)
            .ToListAsync();

        var countById = itemCounts
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return valores.Select(v => new ValorCategoriaListItemDto
        {
            Id = v.Id,
            Descripcion = v.Descripcion,
            IdTipo = v.Tipo?.Id ?? 0,
            Tipo = v.Tipo?.Descripcion ?? string.Empty,
            CantidadItems = countById.GetValueOrDefault(v.Id, 0),
        }).ToList();
    }

    public async Task<ValorCategoriaDetailDto?> GetValorCategoriaDetailAsync(int id)
    {
        var valor = await session.Query<ValorCategoriaCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (valor is null) return null;

        var items = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Where(x => x.ValorCategoriaId == id)
            .OrderBy(x => x.Numero)
            .ToListAsync();

        return new ValorCategoriaDetailDto
        {
            Id = valor.Id,
            Descripcion = valor.Descripcion,
            IdTipo = valor.Tipo?.Id ?? 0,
            Tipo = valor.Tipo?.Descripcion ?? string.Empty,
            Items = items.Select(i => new ValorCategoriaConfiguradoItemDto
            {
                Id = i.Id,
                NumeroCategoria = i.Numero,
                Importe = i.Importe,
            }).ToList(),
        };
    }

    public async Task<ValorCategoriaDetailDto> CreateValorCategoriaAsync(ValorCategoriaCreateUpdateDto dto)
    {
        var entity = new ValorCategoriaCatalogEntity
        {
            Descripcion = dto.Descripcion,
            Tipo = dto.IdTipo > 0 ? session.Load<ValorCategoriaTipoCatalogEntity>(dto.IdTipo) : null,
        };
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        await NHibernateUtil.InitializeAsync(entity.Tipo);
        return new ValorCategoriaDetailDto
        {
            Id = entity.Id,
            Descripcion = entity.Descripcion,
            IdTipo = entity.Tipo?.Id ?? 0,
            Tipo = entity.Tipo?.Descripcion ?? string.Empty,
        };
    }

    public async Task<ValorCategoriaDetailDto?> UpdateValorCategoriaAsync(int id, ValorCategoriaCreateUpdateDto dto)
    {
        var entity = await session.Query<ValorCategoriaCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;
        entity.Tipo = dto.IdTipo > 0 ? session.Load<ValorCategoriaTipoCatalogEntity>(dto.IdTipo) : null;

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return await GetValorCategoriaDetailAsync(id);
    }

    public async Task<bool> DeleteValorCategoriaAsync(int id)
    {
        var inUse = await session.Query<ValorCategoriaConfiguradoEntity>()
            .Where(x => x.ValorCategoriaId == id)
            .CountAsync() > 0;
        if (inUse) return false;

        var items = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Where(x => x.ValorCategoriaId == id)
            .ToListAsync();

        var entity = await session.GetAsync<ValorCategoriaCatalogEntity>(id);
        if (entity is null) return true;

        using var tx = session.BeginTransaction();
        foreach (var item in items)
            await session.DeleteAsync(item);
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<ValorCategoriaConfiguradoItemDto> CreateValorCategoriaItemAsync(
        int valorCategoriaId, ValorCategoriaItemCreateUpdateDto dto)
    {
        var entity = new ValorCategoriaConfiguradoItemEntity
        {
            ValorCategoriaId = valorCategoriaId,
            Numero = dto.NumeroCategoria,
            Importe = dto.Importe,
        };
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return new ValorCategoriaConfiguradoItemDto { Id = entity.Id, NumeroCategoria = entity.Numero, Importe = entity.Importe };
    }

    public async Task<ValorCategoriaConfiguradoItemDto?> UpdateValorCategoriaItemAsync(
        int id, ValorCategoriaItemCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<ValorCategoriaConfiguradoItemEntity>(id);
        if (entity is null) return null;
        entity.Numero = dto.NumeroCategoria;
        entity.Importe = dto.Importe;
        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
        return new ValorCategoriaConfiguradoItemDto { Id = entity.Id, NumeroCategoria = entity.Numero, Importe = entity.Importe };
    }

    public async Task DeleteValorCategoriaItemAsync(int id)
    {
        var entity = await session.GetAsync<ValorCategoriaConfiguradoItemEntity>(id);
        if (entity is null) return;
        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
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

    public async Task<List<ValorCategoriaDetailDto>?> CloneValoresCategoriaMasivoAsync(ClonacionMasivaValoresCategoriaDto dto)
    {
        var ids = dto.ValoresCategoriaIds.Distinct().ToList();

        if (ids.Count == 0) return null;

        const int oracleInLimit = 1000;
        var valores = new List<ValorCategoriaCatalogEntity>(ids.Count);

        for (var i = 0; i < ids.Count; i += oracleInLimit)
        {
            var batch = ids.Skip(i).Take(oracleInLimit).ToList();
            var batchValores = await session.Query<ValorCategoriaCatalogEntity>()
                .Fetch(x => x.Tipo)
                .Where(x => batch.Contains(x.Id))
                .ToListAsync();
            valores.AddRange(batchValores);
        }

        if (valores.Count != ids.Count) return null;

        var items = new List<ValorCategoriaConfiguradoItemEntity>();
        for (var i = 0; i < ids.Count; i += oracleInLimit)
        {
            var batch = ids.Skip(i).Take(oracleInLimit).ToList();
            var batchItems = await session.Query<ValorCategoriaConfiguradoItemEntity>()
                .Where(x => batch.Contains(x.ValorCategoriaId))
                .OrderBy(x => x.ValorCategoriaId)
                .ThenBy(x => x.Numero)
                .ToListAsync();
            items.AddRange(batchItems);
        }

        var itemsByValorCategoriaId = items
            .GroupBy(item => item.ValorCategoriaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (dto.ActualizarValoresExistentes)
        {
            using var tx = session.BeginTransaction();
            foreach (var valor in valores)
            {
                var originalItems = itemsByValorCategoriaId.GetValueOrDefault(valor.Id, []);
                foreach (var item in originalItems)
                    item.Importe = Math.Round(item.Importe * dto.CoeficienteAjuste, 2, MidpointRounding.AwayFromZero);
            }
            await tx.CommitAsync();

            var result = new List<ValorCategoriaDetailDto>(valores.Count);
            for (var i = 0; i < valores.Count; i++)
            {
                var valor = valores[i];
                var originalItems = itemsByValorCategoriaId.GetValueOrDefault(valor.Id, []);
                var itemDtos = originalItems.Select(item => new ValorCategoriaConfiguradoItemDto
                {
                    Id = item.Id,
                    NumeroCategoria = item.Numero,
                    Importe = item.Importe,
                }).ToList();

                result.Add(new ValorCategoriaDetailDto
                {
                    Id = valor.Id,
                    Descripcion = valor.Descripcion,
                    IdTipo = valor.Tipo?.Id ?? 0,
                    Tipo = valor.Tipo?.Descripcion ?? string.Empty,
                    Items = itemDtos,
                });
            }

            return result;
        }
        else
        {
            var clones = valores.Select(v => new ValorCategoriaCatalogEntity
            {
                Descripcion = ReemplazarPeriodoEnDescripcion(v.Descripcion, dto.NuevoPeriodo),
                Tipo = v.Tipo,
            }).ToList();

            using var tx = session.BeginTransaction();
            foreach (var clone in clones)
                await session.SaveAsync(clone);
            await session.FlushAsync();

            var result = new List<ValorCategoriaDetailDto>(clones.Count);
            for (var i = 0; i < valores.Count; i++)
            {
                var clone = clones[i];
                var originalItems = itemsByValorCategoriaId.GetValueOrDefault(valores[i].Id, []);
                var clonedItemDtos = new List<ValorCategoriaConfiguradoItemDto>(originalItems.Count);

                foreach (var item in originalItems)
                {
                    var clonedItem = new ValorCategoriaConfiguradoItemEntity
                    {
                        ValorCategoriaId = clone.Id,
                        Numero = item.Numero,
                        Importe = Math.Round(item.Importe * dto.CoeficienteAjuste, 2, MidpointRounding.AwayFromZero),
                    };
                    await session.SaveAsync(clonedItem);
                    clonedItemDtos.Add(new ValorCategoriaConfiguradoItemDto
                    {
                        Id = clonedItem.Id,
                        NumeroCategoria = clonedItem.Numero,
                        Importe = clonedItem.Importe,
                    });
                }

                result.Add(new ValorCategoriaDetailDto
                {
                    Id = clone.Id,
                    Descripcion = clone.Descripcion,
                    IdTipo = clone.Tipo?.Id ?? 0,
                    Tipo = clone.Tipo?.Descripcion ?? string.Empty,
                    Items = clonedItemDtos,
                });
            }
            await tx.CommitAsync();

            return result;
        }

    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetValorFijoTiposAsync()
    {
        var items = await session.Query<ValorFijoTipoCatalogEntity>()
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Descripcion)
            .ToListAsync();

        return items.Select(item => new CatalogItemDto
        {
            Id = item.Id,
            Descripcion = item.Descripcion
        }).ToList();
    }

    public async Task<CatalogItemDto> CreateValorFijoTipoAsync(CatalogItemDto dto)
    {
        var entity = new ValorFijoTipoCatalogEntity
        {
            Descripcion = dto.Descripcion
        };
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return new CatalogItemDto { Id = entity.Id, Descripcion = entity.Descripcion };
    }

    public async Task<CatalogItemDto?> UpdateValorFijoTipoAsync(int id, CatalogItemDto dto)
    {
        var entity = await session.GetAsync<ValorFijoTipoCatalogEntity>(id);
        if (entity is null) return null;
        entity.Descripcion = dto.Descripcion;
        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
        return new CatalogItemDto { Id = entity.Id, Descripcion = entity.Descripcion };
    }

    public async Task<bool> DeleteValorFijoTipoAsync(int id)
    {
        var inUse = await session.Query<ValorFijoCatalogEntity>()
            .Where(x => x.Tipo != null && x.Tipo.Id == id)
            .CountAsync() > 0;
        if (inUse) return false;

        var entity = await session.GetAsync<ValorFijoTipoCatalogEntity>(id);
        if (entity is null) return true;
        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return true;
    }

    // ── Grupos de tipos de valor fijo ───────────────────────────────────────────
    // Agrupan tipos (no valores puntuales) para que la clonación masiva mensual
    // no requiera re-seleccionar los mismos tipos cada vez.

    public async Task<IReadOnlyCollection<GrupoValorFijoDto>> GetGruposValorFijoAsync()
    {
        var items = await session.Query<GrupoValorFijoEntity>()
            .OrderBy(x => x.Descripcion)
            .ToListAsync();

        foreach (var item in items)
            await NHibernateUtil.InitializeAsync(item.Tipos);

        return items.Select(ToGrupoValorFijoDto).ToList();
    }

    public async Task<GrupoValorFijoDto?> GetGrupoValorFijoByIdAsync(int id)
    {
        var entity = await session.Query<GrupoValorFijoEntity>()
            .Fetch(x => x.Tipos)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity is null ? null : ToGrupoValorFijoDto(entity);
    }

    public async Task<GrupoValorFijoDto> CreateGrupoValorFijoAsync(GrupoValorFijoCreateUpdateDto dto)
    {
        var entity = new GrupoValorFijoEntity { Descripcion = dto.Descripcion };
        foreach (var tipoId in dto.TiposIds.Distinct())
            entity.Tipos.Add(session.Load<ValorFijoTipoCatalogEntity>(tipoId));

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        await NHibernateUtil.InitializeAsync(entity.Tipos);
        return ToGrupoValorFijoDto(entity);
    }

    public async Task<GrupoValorFijoDto?> UpdateGrupoValorFijoAsync(int id, GrupoValorFijoCreateUpdateDto dto)
    {
        var entity = await session.Query<GrupoValorFijoEntity>()
            .Fetch(x => x.Tipos)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;

        // Los Tipos ya son entidades persistentes cargadas por Load: reemplazar la
        // colección así solo reescribe filas de la tabla de unión, no las entidades.
        entity.Tipos.Clear();
        foreach (var tipoId in dto.TiposIds.Distinct())
            entity.Tipos.Add(session.Load<ValorFijoTipoCatalogEntity>(tipoId));

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToGrupoValorFijoDto(entity);
    }

    public async Task<bool> DeleteGrupoValorFijoAsync(int id)
    {
        var entity = await session.GetAsync<GrupoValorFijoEntity>(id);
        if (entity is null) return true;

        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return true;
    }

    private static GrupoValorFijoDto ToGrupoValorFijoDto(GrupoValorFijoEntity entity) => new()
    {
        Id = entity.Id,
        Descripcion = entity.Descripcion,
        Tipos = entity.Tipos
            .Select(t => new CatalogItemDto { Id = t.Id, Descripcion = t.Descripcion })
            .OrderBy(t => t.Id).ThenBy(t => t.Descripcion)
            .ToList(),
    };

    // ── Grupos de tipos de valor por categoría ──────────────────────────────────
    // Agrupan tipos (no valores puntuales) para acotar rápido la lista de tipos
    // al asociar/gestionar valores por categoría, igual que los grupos de valor fijo.

    public async Task<IReadOnlyCollection<GrupoValorCategoriaDto>> GetGruposValorCategoriaAsync()
    {
        var items = await session.Query<GrupoValorCategoriaEntity>()
            .OrderBy(x => x.Descripcion)
            .ToListAsync();

        foreach (var item in items)
            await NHibernateUtil.InitializeAsync(item.Tipos);

        return items.Select(ToGrupoValorCategoriaDto).ToList();
    }

    public async Task<GrupoValorCategoriaDto?> GetGrupoValorCategoriaByIdAsync(int id)
    {
        var entity = await session.Query<GrupoValorCategoriaEntity>()
            .Fetch(x => x.Tipos)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity is null ? null : ToGrupoValorCategoriaDto(entity);
    }

    public async Task<GrupoValorCategoriaDto> CreateGrupoValorCategoriaAsync(GrupoValorCategoriaCreateUpdateDto dto)
    {
        var entity = new GrupoValorCategoriaEntity { Descripcion = dto.Descripcion };
        foreach (var tipoId in dto.TiposIds.Distinct())
            entity.Tipos.Add(session.Load<ValorCategoriaTipoCatalogEntity>(tipoId));

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        await NHibernateUtil.InitializeAsync(entity.Tipos);
        return ToGrupoValorCategoriaDto(entity);
    }

    public async Task<GrupoValorCategoriaDto?> UpdateGrupoValorCategoriaAsync(int id, GrupoValorCategoriaCreateUpdateDto dto)
    {
        var entity = await session.Query<GrupoValorCategoriaEntity>()
            .Fetch(x => x.Tipos)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;

        // Los Tipos ya son entidades persistentes cargadas por Load: reemplazar la
        // colección así solo reescribe filas de la tabla de unión, no las entidades.
        entity.Tipos.Clear();
        foreach (var tipoId in dto.TiposIds.Distinct())
            entity.Tipos.Add(session.Load<ValorCategoriaTipoCatalogEntity>(tipoId));

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToGrupoValorCategoriaDto(entity);
    }

    public async Task<bool> DeleteGrupoValorCategoriaAsync(int id)
    {
        var entity = await session.GetAsync<GrupoValorCategoriaEntity>(id);
        if (entity is null) return true;

        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
        return true;
    }

    private static GrupoValorCategoriaDto ToGrupoValorCategoriaDto(GrupoValorCategoriaEntity entity) => new()
    {
        Id = entity.Id,
        Descripcion = entity.Descripcion,
        Tipos = entity.Tipos
            .Select(t => new CatalogItemDto { Id = t.Id, Descripcion = t.Descripcion })
            .OrderBy(t => t.Id).ThenBy(t => t.Descripcion)
            .ToList(),
    };

    public Task<IReadOnlyCollection<ValorFijoCatalogDto>> GetAllValoresFijosListAsync()
        => GetValoresFijosAsync();

    public async Task<ValorFijoCatalogDto?> GetValorFijoDetailAsync(int id)
    {
        var valor = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (valor is null) return null;

        return ToValorFijoCatalogDto(valor);
    }

    public async Task<bool> DeleteValorFijoAsync(int id)
    {
        var inUse = await session.Query<ValorFijoConfiguradoEntity>()
            .Where(x => x.ValorFijoId == id)
            .CountAsync() > 0;
        if (inUse) return false;

        var entity = await session.GetAsync<ValorFijoCatalogEntity>(id);
        if (entity is null) return true;

        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();

        return true;
    }

    public async Task<ValorFijoCatalogDto?> UpdateValorFijoAsync(int id, ValorFijoCreateDto dto)
    {
        var entity = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return null;

        entity.Descripcion = dto.Descripcion;
        entity.Tipo = session.Load<ValorFijoTipoCatalogEntity>(dto.IdTipo);
        entity.Valor = dto.Valor;

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToValorFijoCatalogDto(entity);
    }

    public async Task<ValorFijoCatalogDto?> CloneValorFijoAsync(int id, ValorFijoCloneDto dto)
    {
        var entity = await session.Query<ValorFijoCatalogEntity>()
            .Fetch(x => x.Tipo)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null) return null;

        var clone = new ValorFijoCatalogEntity
        {
            Descripcion = dto.Descripcion.Trim(),
            Tipo = entity.Tipo,
            Valor = Math.Round(entity.Valor * dto.CoeficienteAjuste, 2, MidpointRounding.AwayFromZero)
        };

        using var tx = session.BeginTransaction();
        await session.SaveAsync(clone);
        await session.FlushAsync();
        await tx.CommitAsync();

        return ToValorFijoCatalogDto(clone);
    }

    public async Task<List<ValorFijoCatalogDto>?> CloneValoresFijosMasivoAsync(ClonacionMasivaValoresFijosDto dto)
    {
        var ids = dto.ValoresFijosIds.Distinct().ToList();

        if (ids.Count == 0) return null;

        const int oracleInLimit = 1000;
        var valores = new List<ValorFijoCatalogEntity>(ids.Count);

        for (var i = 0; i < ids.Count; i += oracleInLimit)
        {
            var batch = ids.Skip(i).Take(oracleInLimit).ToList();
            var batchValores = await session.Query<ValorFijoCatalogEntity>()
                .Fetch(x => x.Tipo)
                .Where(x => batch.Contains(x.Id))
                .ToListAsync();
            valores.AddRange(batchValores);
        }

        if (valores.Count != ids.Count) return null;

        if (valores.Count == 0) return null;

        if (dto.ActualizarValoresExistentes)
        {
            using var tx = session.BeginTransaction();
            foreach (var valor in valores)
            {
                valor.Valor = Math.Round(valor.Valor * dto.CoeficienteAjuste, 2, MidpointRounding.AwayFromZero);
            }
            await tx.CommitAsync();
            return valores.Select(ToValorFijoCatalogDto).ToList();
        }
        else
        {
            var clones = valores.Select(v => new ValorFijoCatalogEntity
            {
                Descripcion = ReemplazarPeriodoEnDescripcion(v.Descripcion, dto.NuevoPeriodo),
                Tipo = v.Tipo,
                Valor = Math.Round(v.Valor * dto.CoeficienteAjuste, 2, MidpointRounding.AwayFromZero)
            }).ToList();

            using var tx = session.BeginTransaction();
            foreach (var clone in clones)
            {
                await session.SaveAsync(clone);
            }
            await tx.CommitAsync();
            return clones.Select(ToValorFijoCatalogDto).ToList();
        }
    }

    public async Task TestDatabaseConnectionAsync()
    {
        await session.CreateSQLQuery("SELECT 1 AS ok FROM dual")
            .AddScalar("ok", NHibernateUtil.Int32)
            .UniqueResultAsync<int>();
    }
}

