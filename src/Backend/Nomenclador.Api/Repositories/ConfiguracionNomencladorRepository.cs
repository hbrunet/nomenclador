using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class ConfiguracionNomencladorRepository(NHibernate.ISession session)
{
    private const int OracleInClauseChunkSize = 900;

    public async Task<(IReadOnlyCollection<ConfiguracionNomencladorEntity> Items, int Total)> GetAllAsync(
        int? nomencladorId,
        int? escalaSalarialId,
        int? zonaId,
        DateOnly? vigenteEn,
        string? estado,
        int page,
        int pageSize)
    {
        ConfiguracionNomencladorEntity alias = null!;
        NomencladorCatalogEntity nomencladorAlias = null!;
        var query = session.QueryOver(() => alias)
            .JoinAlias(() => alias.Nomenclador, () => nomencladorAlias);

        if (nomencladorId.HasValue)
            query.Where(() => alias.NomencladorId == nomencladorId.Value);

        if (escalaSalarialId.HasValue)
            query.Where(() => alias.EscalaSalarialId == escalaSalarialId.Value);

        if (zonaId.HasValue)
            query.Where(() => alias.ZonaId == zonaId.Value);

        if (vigenteEn.HasValue)
        {
            var fecha = vigenteEn.Value;

            query.Where(Restrictions.Le(
                Projections.Property(() => alias.FechaInicio), fecha));
            query.Where(Restrictions.Ge(
                Projections.Property(() => alias.FechaFin), fecha));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            switch (estado.Trim().ToUpperInvariant())
            {
                case "FUTURA":
                    query.Where(Restrictions.Gt(
                        Projections.Property(() => alias.FechaInicio), today));
                    break;
                case "VENCIDA":
                    query.Where(Restrictions.And(
                        Restrictions.IsNotNull(Projections.Property(() => alias.FechaFin)),
                        Restrictions.Lt(Projections.Property(() => alias.FechaFin), today)));
                    break;
                case "ACTIVA":
                    query.Where(Restrictions.Le(
                        Projections.Property(() => alias.FechaInicio), today));
                    query.Where(Restrictions.Ge(
                        Projections.Property(() => alias.FechaFin), today));
                    break;
            }
        }

        var total = await query.RowCountAsync();

        var rawItems = await query
            .OrderBy(() => alias.FechaInicio).Desc
            .ThenBy(() => nomencladorAlias.Descripcion).Asc
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ListAsync();

        return (rawItems.ToList(), total);
    }

    public async Task<ConfiguracionNomencladorEntity?> GetByIdAsync(int id)
    {
        var entity = await session.GetAsync<ConfiguracionNomencladorEntity>(id);
        if (entity == null) return null;

        await LoadValorCategoriaItemsAsync(entity);

        return entity;
    }

    private async Task LoadValorCategoriaItemsAsync(ConfiguracionNomencladorEntity entity)
    {
        var ids = entity.ValoresCategorias
            .Select(vc => vc.ValorCategoriaId)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return;

        var allItems = await session.Query<ValorCategoriaConfiguradoItemEntity>()
            .Where(item => ids.Contains(item.ValorCategoriaId))
            .ToListAsync();

        var byId = allItems
            .GroupBy(item => item.ValorCategoriaId)
            .ToDictionary(g => g.Key, g => (IList<ValorCategoriaConfiguradoItemEntity>)g.ToList());

        foreach (var vc in entity.ValoresCategorias)
        {
            vc.Items = byId.TryGetValue(vc.ValorCategoriaId, out var items) ? items : [];
        }
    }

    public async Task AddAsync(ConfiguracionNomencladorEntity entity, Action<ConfiguracionNomencladorEntity>? afterSave = null)
    {
        using var tx = session.BeginTransaction();
        // SaveAsync hace que la secuencia de Oracle asigne entity.Id de inmediato,
        // de modo que el callback puede usarlo (p. ej. para setear las FK de los hijos).
        await session.SaveAsync(entity);
        afterSave?.Invoke(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
    }

    public async Task AddConceptoAsync(int configuracionId, Nomenclador.Api.DTOs.ConceptoConfiguradoInputDto request)
    {
        using var tx = session.BeginTransaction();

        var configuracion = await session.GetAsync<ConfiguracionNomencladorEntity>(configuracionId);
        if (configuracion is not null)
        {
            if (NHibernateUtil.IsInitialized(configuracion.Conceptos))
            {
                // Collection already in memory: use it to avoid extra DB round-trips and keep
                // the in-memory cache up to date.
                if (!configuracion.Conceptos.Any(c => c.ConceptoId == request.IdConcepto))
                {
                    configuracion.Conceptos.Add(new ConceptoConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ConceptoId = request.IdConcepto,
                        Orden = configuracion.Conceptos.Count + 1,
                    });
                    await session.FlushAsync();
                }
            }
            else
            {
                // Collection not yet loaded: check and insert at DB level to avoid forcing a full
                // collection load just to verify existence.
                var yaExiste = await session.Query<ConceptoConfiguradoEntity>()
                    .AnyAsync(c => c.ConfiguracionNomencladorId == configuracionId && c.ConceptoId == request.IdConcepto);

                if (!yaExiste)
                {
                    var siguienteOrden = await session.Query<ConceptoConfiguradoEntity>()
                        .CountAsync(c => c.ConfiguracionNomencladorId == configuracionId) + 1;

                    await session.SaveAsync(new ConceptoConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ConceptoId = request.IdConcepto,
                        Orden = siguienteOrden,
                    });
                    await session.FlushAsync();
                }
            }
        }

        await tx.CommitAsync();
    }

    public async Task RemoveConceptoAsync(int configuracionId, int conceptoId)
    {
        using var tx = session.BeginTransaction();

        var entity = await session.Query<ConceptoConfiguradoEntity>()
            .FirstOrDefaultAsync(c => c.ConfiguracionNomencladorId == configuracionId && c.ConceptoId == conceptoId);

        if (entity is not null)
        {
            await session.DeleteAsync(entity);
            await session.FlushAsync();
        }

        // Renumerar el orden de los conceptos restantes para que sea consecutivo.
        var restantes = await session.Query<ConceptoConfiguradoEntity>()
            .Where(c => c.ConfiguracionNomencladorId == configuracionId)
            .OrderBy(c => c.Orden)
            .ToListAsync();

        for (var i = 0; i < restantes.Count; i++)
        {
            restantes[i].Orden = i + 1;
        }

        await tx.CommitAsync();
    }

    public async Task AddValorFijoAsync(int configuracionId, Nomenclador.Api.DTOs.ValorFijoConfiguradoInputDto request)
    {
        using var tx = session.BeginTransaction();

        var configuracion = await session.GetAsync<ConfiguracionNomencladorEntity>(configuracionId);
        if (configuracion is not null)
        {
            if (NHibernateUtil.IsInitialized(configuracion.ValoresFijos))
            {
                // Collection already in memory: use it to avoid an extra DB round-trip and keep
                // the in-memory cache up to date.
                if (!configuracion.ValoresFijos.Any(v => v.ValorFijoId == request.IdValorFijo))
                {
                    configuracion.ValoresFijos.Add(new ValorFijoConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ValorFijoId = request.IdValorFijo,
                    });
                    await session.FlushAsync();
                }
            }
            else
            {
                // Collection not yet loaded: check and insert at DB level to avoid forcing a full
                // collection load just to verify existence.
                var yaExiste = await session.Query<ValorFijoConfiguradoEntity>()
                    .AnyAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorFijoId == request.IdValorFijo);

                if (!yaExiste)
                {
                    await session.SaveAsync(new ValorFijoConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ValorFijoId = request.IdValorFijo,
                    });
                    await session.FlushAsync();
                }
            }
        }

        await tx.CommitAsync();
    }

    public async Task RemoveValorFijoAsync(int configuracionId, int valorFijoId)
    {
        using var tx = session.BeginTransaction();

        var entity = await session.Query<ValorFijoConfiguradoEntity>()
            .FirstOrDefaultAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorFijoId == valorFijoId);

        if (entity is not null)
        {
            await session.DeleteAsync(entity);
            await session.FlushAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<int> AsociarValoresFijosMasivoAsync(
        IReadOnlyCollection<int> configuracionIds,
        IReadOnlyCollection<int> valorFijoIds)
    {
        if (configuracionIds.Count == 0 || valorFijoIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var existentes = await session.Query<ValorFijoConfiguradoEntity>()
            .Where(v => configuracionIds.Contains(v.ConfiguracionNomencladorId) && valorFijoIds.Contains(v.ValorFijoId))
            .Select(v => new { v.ConfiguracionNomencladorId, v.ValorFijoId })
            .ToListAsync();

        var existentesSet = existentes
            .Select(e => (e.ConfiguracionNomencladorId, e.ValorFijoId))
            .ToHashSet();

        var creadas = 0;
        foreach (var configuracionId in configuracionIds)
        {
            foreach (var valorFijoId in valorFijoIds)
            {
                if (!existentesSet.Add((configuracionId, valorFijoId))) continue;

                await session.SaveAsync(new ValorFijoConfiguradoEntity
                {
                    ConfiguracionNomencladorId = configuracionId,
                    ValorFijoId = valorFijoId,
                });
                creadas++;
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return creadas;
    }

    public async Task<int> DesasociarValoresFijosMasivoAsync(
        IReadOnlyCollection<int> configuracionIds,
        IReadOnlyCollection<int> valorFijoIds)
    {
        if (configuracionIds.Count == 0 || valorFijoIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var existentes = await session.Query<ValorFijoConfiguradoEntity>()
            .Where(v => configuracionIds.Contains(v.ConfiguracionNomencladorId) && valorFijoIds.Contains(v.ValorFijoId))
            .ToListAsync();

        foreach (var entity in existentes)
        {
            await session.DeleteAsync(entity);
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return existentes.Count;
    }

    public async Task AddValorPorCategoriaAsync(int configuracionId, Nomenclador.Api.DTOs.ValorCategoriaConfiguradoInputDto request)
    {
        using var tx = session.BeginTransaction();

        var configuracion = await session.GetAsync<ConfiguracionNomencladorEntity>(configuracionId);
        if (configuracion is not null)
        {
            if (NHibernateUtil.IsInitialized(configuracion.ValoresCategorias))
            {
                // Collection already in memory (e.g. loaded earlier in this session): use it to
                // avoid an extra DB round-trip and keep the in-memory cache up to date.
                if (!configuracion.ValoresCategorias.Any(v => v.ValorCategoriaId == request.IdValorCategoria))
                {
                    configuracion.ValoresCategorias.Add(new ValorCategoriaConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ValorCategoriaId = request.IdValorCategoria,
                    });
                    await session.FlushAsync();
                }
            }
            else
            {
                // Collection not yet loaded: check and insert at DB level to avoid forcing a full
                // collection load just to verify existence.
                var yaExiste = await session.Query<ValorCategoriaConfiguradoEntity>()
                    .AnyAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorCategoriaId == request.IdValorCategoria);

                if (!yaExiste)
                {
                    await session.SaveAsync(new ValorCategoriaConfiguradoEntity
                    {
                        ConfiguracionNomencladorId = configuracionId,
                        ValorCategoriaId = request.IdValorCategoria,
                    });
                    await session.FlushAsync();
                }
            }
        }

        await tx.CommitAsync();
    }

    public async Task RemoveValorPorCategoriaAsync(int configuracionId, int valorCategoriaId)
    {
        using var tx = session.BeginTransaction();

        // La colección ValoresCategorias del padre está mapeada con Cascade.AllDeleteOrphan().
        // Si el padre ya fue cargado en esta sesión (p. ej. por GetByIdAsync), el ítem también
        // queda referenciado en esa colección. Eliminarlo directamente con session.DeleteAsync
        // mientras sigue en la colección provoca "deleted object would be re-saved by cascade".
        // Por eso se remueve de la colección del padre para que el cascade emita el DELETE.
        var configuracion = await session.GetAsync<ConfiguracionNomencladorEntity>(configuracionId);
        var entity = configuracion?.ValoresCategorias
            .FirstOrDefault(v => v.ValorCategoriaId == valorCategoriaId);

        if (configuracion is not null && entity is not null)
        {
            configuracion.ValoresCategorias.Remove(entity);
            await session.FlushAsync();
        }

        await tx.CommitAsync();
    }

    public async Task SaveChangesAsync()
    {
        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
    }

    public async Task<bool> HasOverlapAsync(ConfiguracionNomencladorEntity entity, int? excludedId)
    {
        ConfiguracionNomencladorEntity alias = null!;
        var query = session.QueryOver(() => alias)
            .Where(() => alias.NomencladorId == entity.NomencladorId);

        if (excludedId.HasValue)
        {
            var idAExcluir = excludedId.Value;
            query.Where(() => alias.Id != idAExcluir);
        }

        var candidatos = await query.ListAsync();

        return candidatos.Any(c =>
            RangesOverlap(c.FechaInicio, c.FechaFin, entity.FechaInicio, entity.FechaFin));
    }

    private static bool RangesOverlap(DateOnly firstStart, DateOnly? firstEnd,
                                       DateOnly secondStart, DateOnly? secondEnd)
    {
        var normalizedFirstEnd = firstEnd ?? DateOnly.MaxValue;
        var normalizedSecondEnd = secondEnd ?? DateOnly.MaxValue;
        return firstStart <= normalizedSecondEnd && secondStart <= normalizedFirstEnd;
    }

    public async Task<int> AsociarValoresCategoriasMasivoAsync(
        IReadOnlyCollection<int> configuracionIds,
        IReadOnlyCollection<int> valoresCategoriasIds)
    {
        if (configuracionIds.Count == 0 || valoresCategoriasIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var existentesSet = new HashSet<(int ConfiguracionNomencladorId, int ValorCategoriaId)>();
        foreach (var configuracionesChunk in GetChunks(configuracionIds))
        {
            foreach (var valoresCategoriasChunk in GetChunks(valoresCategoriasIds))
            {
                var existentesChunk = await session.Query<ValorCategoriaConfiguradoEntity>()
                    .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId) && valoresCategoriasChunk.Contains(v.ValorCategoriaId))
                    .Select(v => new { v.ConfiguracionNomencladorId, v.ValorCategoriaId })
                    .ToListAsync();

                foreach (var existente in existentesChunk)
                {
                    existentesSet.Add((existente.ConfiguracionNomencladorId, existente.ValorCategoriaId));
                }
            }
        }

        var creadas = 0;
        foreach (var configuracionId in configuracionIds)
        {
            foreach (var valorCategoriaId in valoresCategoriasIds)
            {
                if (!existentesSet.Add((configuracionId, valorCategoriaId))) continue;

                await session.SaveAsync(new ValorCategoriaConfiguradoEntity
                {
                    ConfiguracionNomencladorId = configuracionId,
                    ValorCategoriaId = valorCategoriaId,
                });
                creadas++;
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return creadas;
    }

    public async Task<int> DesasociarValoresCategoriasMasivoAsync(IReadOnlyCollection<int> configuracionesIds, IReadOnlyCollection<int> valoresCategoriasIds)
    {
        if (configuracionesIds.Count == 0 || valoresCategoriasIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var eliminadas = 0;
        foreach (var configuracionesChunk in GetChunks(configuracionesIds))
        {
            foreach (var valoresCategoriasChunk in GetChunks(valoresCategoriasIds))
            {
                var existentesChunk = await session.Query<ValorCategoriaConfiguradoEntity>()
                    .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId) && valoresCategoriasChunk.Contains(v.ValorCategoriaId))
                    .ToListAsync();

                foreach (var entity in existentesChunk)
                {
                    await session.DeleteAsync(entity);
                    eliminadas++;
                }
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return eliminadas;
    }

    private static IEnumerable<int[]> GetChunks(IReadOnlyCollection<int> ids)
    {
        var distinctIds = ids.Distinct().ToArray();
        for (var i = 0; i < distinctIds.Length; i += OracleInClauseChunkSize)
        {
            yield return distinctIds.Skip(i).Take(OracleInClauseChunkSize).ToArray();
        }
    }

    public async Task<int> AsociarConceptosMasivoAsync(IReadOnlyCollection<int> configuracionesIds, IReadOnlyCollection<int> conceptosIds)
    {
        if (configuracionesIds.Count == 0 || conceptosIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var existentes = await session.Query<ConceptoConfiguradoEntity>()
            .Where(v => configuracionesIds.Contains(v.ConfiguracionNomencladorId) && conceptosIds.Contains(v.ConceptoId))
            .Select(v => new { v.ConfiguracionNomencladorId, v.ConceptoId })
            .ToListAsync();

        var existentesSet = existentes
            .Select(e => (e.ConfiguracionNomencladorId, e.ConceptoId))
            .ToHashSet();

        var creadas = 0;
        foreach (var configuracionId in configuracionesIds)
        {
            foreach (var conceptoId in conceptosIds)
            {
                if (!existentesSet.Add((configuracionId, conceptoId))) continue;

                await session.SaveAsync(new ConceptoConfiguradoEntity
                {
                    ConfiguracionNomencladorId = configuracionId,
                    ConceptoId = conceptoId,
                });
                creadas++;
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return creadas;
    }

    public async Task<int> DesasociarConceptosMasivoAsync(IReadOnlyCollection<int> configuracionesIds, IReadOnlyCollection<int> conceptosIds)
    {
        if (configuracionesIds.Count == 0 || conceptosIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var eliminadas = 0;
        foreach (var configuracionesChunk in GetChunks(configuracionesIds))
        {
            foreach (var conceptosChunk in GetChunks(conceptosIds))
            {
                var existentesChunk = await session.Query<ConceptoConfiguradoEntity>()
                    .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId) && conceptosChunk.Contains(v.ConceptoId))
                    .ToListAsync();

                foreach (var entity in existentesChunk)
                {
                    await session.DeleteAsync(entity);
                    eliminadas++;
                }
            }
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return eliminadas;
    }
}
