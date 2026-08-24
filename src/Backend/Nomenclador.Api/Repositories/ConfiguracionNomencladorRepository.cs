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
            .ThenBy(() => nomencladorAlias.Id).Asc
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
            await ReemplazarValorFijoPorTipoAsync(configuracion, configuracionId, request.IdValorFijo);
            await session.FlushAsync();
        }

        await tx.CommitAsync();
    }

    // Una configuración solo puede tener un valor fijo asociado por tipo: si ya hay uno del
    // mismo tipo que el solicitado, se lo quita y se asocia el nuevo en su lugar, en vez de
    // bloquear la operación.
    private async Task ReemplazarValorFijoPorTipoAsync(ConfiguracionNomencladorEntity configuracion, int configuracionId, int valorFijoId)
    {
        var coleccionCargada = NHibernateUtil.IsInitialized(configuracion.ValoresFijos);

        var existentesIds = coleccionCargada
            ? configuracion.ValoresFijos.Select(v => v.ValorFijoId).ToList()
            : await session.Query<ValorFijoConfiguradoEntity>()
                .Where(v => v.ConfiguracionNomencladorId == configuracionId)
                .Select(v => v.ValorFijoId)
                .ToListAsync();

        if (existentesIds.Contains(valorFijoId)) return;

        var tipoIds = await GetValorFijoTipoIdsAsync([.. existentesIds, valorFijoId]);
        var nuevoTipoId = tipoIds.GetValueOrDefault(valorFijoId);
        var idConflicto = nuevoTipoId.HasValue
            ? existentesIds.FirstOrDefault(id => tipoIds.GetValueOrDefault(id) == nuevoTipoId, 0)
            : 0;

        if (coleccionCargada)
        {
            if (idConflicto != 0)
            {
                var existente = configuracion.ValoresFijos.First(v => v.ValorFijoId == idConflicto);
                configuracion.ValoresFijos.Remove(existente);
            }

            configuracion.ValoresFijos.Add(new ValorFijoConfiguradoEntity
            {
                ConfiguracionNomencladorId = configuracionId,
                ValorFijoId = valorFijoId,
            });
        }
        else
        {
            if (idConflicto != 0)
            {
                var existente = await session.Query<ValorFijoConfiguradoEntity>()
                    .FirstOrDefaultAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorFijoId == idConflicto);
                if (existente is not null)
                    await session.DeleteAsync(existente);
            }

            await session.SaveAsync(new ValorFijoConfiguradoEntity
            {
                ConfiguracionNomencladorId = configuracionId,
                ValorFijoId = valorFijoId,
            });
        }
    }

    // Devuelve, para cada IdValorFijo, el Id de su tipo (null si no tiene tipo asignado).
    private async Task<Dictionary<int, int?>> GetValorFijoTipoIdsAsync(IEnumerable<int> valorFijoIds)
    {
        var ids = valorFijoIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var result = new Dictionary<int, int?>();
        foreach (var chunk in GetChunks(ids))
        {
            var rows = await session.Query<ValorFijoCatalogEntity>()
                .Where(x => chunk.Contains(x.Id))
                .Select(x => new { x.Id, TipoId = x.Tipo == null ? (int?)null : x.Tipo.Id })
                .ToListAsync();

            foreach (var row in rows)
                result[row.Id] = row.TipoId;
        }

        return result;
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

        // Se traen TODAS las asociaciones existentes de estas configuraciones (no solo las de
        // los valores solicitados): hacen falta para detectar conflictos de tipo contra valores
        // ya asociados que no forman parte de esta tanda.
        var asociadosPorConfiguracion = new Dictionary<int, HashSet<int>>();
        foreach (var configuracionesChunk in GetChunks(configuracionIds))
        {
            var existentesChunk = await session.Query<ValorFijoConfiguradoEntity>()
                .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId))
                .Select(v => new { v.ConfiguracionNomencladorId, v.ValorFijoId })
                .ToListAsync();

            foreach (var e in existentesChunk)
            {
                if (!asociadosPorConfiguracion.TryGetValue(e.ConfiguracionNomencladorId, out var set))
                    asociadosPorConfiguracion[e.ConfiguracionNomencladorId] = set = [];
                set.Add(e.ValorFijoId);
            }
        }

        var todosLosIds = asociadosPorConfiguracion.Values.SelectMany(s => s).Concat(valorFijoIds);
        var tipoIds = await GetValorFijoTipoIdsAsync(todosLosIds);

        var creadas = 0;
        foreach (var configuracionId in configuracionIds)
        {
            if (!asociadosPorConfiguracion.TryGetValue(configuracionId, out var asociados))
                asociadosPorConfiguracion[configuracionId] = asociados = [];

            foreach (var valorFijoId in valorFijoIds)
            {
                if (!asociados.Add(valorFijoId)) continue;

                // Un valor fijo de un tipo ya asociado no bloquea la asociación: reemplaza al
                // existente de ese mismo tipo en vez de convivir con él.
                var nuevoTipoId = tipoIds.GetValueOrDefault(valorFijoId);
                var idConflicto = nuevoTipoId.HasValue
                    ? asociados.FirstOrDefault(id => id != valorFijoId && tipoIds.GetValueOrDefault(id) == nuevoTipoId, 0)
                    : 0;

                if (idConflicto != 0)
                {
                    asociados.Remove(idConflicto);
                    var existente = await session.Query<ValorFijoConfiguradoEntity>()
                        .FirstOrDefaultAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorFijoId == idConflicto);
                    if (existente is not null)
                        await session.DeleteAsync(existente);
                }

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
            await ReemplazarValorPorCategoriaPorTipoAsync(configuracion, configuracionId, request.IdValorCategoria);
            await session.FlushAsync();
        }

        await tx.CommitAsync();
    }

    // Una configuración solo puede tener un valor por categoría asociado por tipo: si ya hay uno
    // del mismo tipo que el solicitado, se lo quita y se asocia el nuevo en su lugar, en vez de
    // bloquear la operación.
    private async Task ReemplazarValorPorCategoriaPorTipoAsync(ConfiguracionNomencladorEntity configuracion, int configuracionId, int valorCategoriaId)
    {
        var coleccionCargada = NHibernateUtil.IsInitialized(configuracion.ValoresCategorias);

        var existentesIds = coleccionCargada
            ? configuracion.ValoresCategorias.Select(v => v.ValorCategoriaId).ToList()
            : await session.Query<ValorCategoriaConfiguradoEntity>()
                .Where(v => v.ConfiguracionNomencladorId == configuracionId)
                .Select(v => v.ValorCategoriaId)
                .ToListAsync();

        if (existentesIds.Contains(valorCategoriaId)) return;

        var tipoIds = await GetValorCategoriaTipoIdsAsync([.. existentesIds, valorCategoriaId]);
        var nuevoTipoId = tipoIds.GetValueOrDefault(valorCategoriaId);
        var idConflicto = nuevoTipoId.HasValue
            ? existentesIds.FirstOrDefault(id => tipoIds.GetValueOrDefault(id) == nuevoTipoId, 0)
            : 0;

        if (coleccionCargada)
        {
            if (idConflicto != 0)
            {
                var existente = configuracion.ValoresCategorias.First(v => v.ValorCategoriaId == idConflicto);
                configuracion.ValoresCategorias.Remove(existente);
            }

            configuracion.ValoresCategorias.Add(new ValorCategoriaConfiguradoEntity
            {
                ConfiguracionNomencladorId = configuracionId,
                ValorCategoriaId = valorCategoriaId,
            });
        }
        else
        {
            if (idConflicto != 0)
            {
                var existente = await session.Query<ValorCategoriaConfiguradoEntity>()
                    .FirstOrDefaultAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorCategoriaId == idConflicto);
                if (existente is not null)
                    await session.DeleteAsync(existente);
            }

            await session.SaveAsync(new ValorCategoriaConfiguradoEntity
            {
                ConfiguracionNomencladorId = configuracionId,
                ValorCategoriaId = valorCategoriaId,
            });
        }
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

        // Se traen TODAS las asociaciones existentes de estas configuraciones (no solo las de
        // los valores solicitados): hacen falta para detectar conflictos de tipo contra valores
        // ya asociados que no forman parte de esta tanda.
        var asociadosPorConfiguracion = new Dictionary<int, HashSet<int>>();
        foreach (var configuracionesChunk in GetChunks(configuracionIds))
        {
            var existentesChunk = await session.Query<ValorCategoriaConfiguradoEntity>()
                .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId))
                .Select(v => new { v.ConfiguracionNomencladorId, v.ValorCategoriaId })
                .ToListAsync();

            foreach (var e in existentesChunk)
            {
                if (!asociadosPorConfiguracion.TryGetValue(e.ConfiguracionNomencladorId, out var set))
                    asociadosPorConfiguracion[e.ConfiguracionNomencladorId] = set = [];
                set.Add(e.ValorCategoriaId);
            }
        }

        var todosLosIds = asociadosPorConfiguracion.Values.SelectMany(s => s).Concat(valoresCategoriasIds);
        var tipoIds = await GetValorCategoriaTipoIdsAsync(todosLosIds);

        var creadas = 0;
        foreach (var configuracionId in configuracionIds)
        {
            if (!asociadosPorConfiguracion.TryGetValue(configuracionId, out var asociados))
                asociadosPorConfiguracion[configuracionId] = asociados = [];

            foreach (var valorCategoriaId in valoresCategoriasIds)
            {
                if (!asociados.Add(valorCategoriaId)) continue;

                // Un valor por categoría de un tipo ya asociado no bloquea la asociación:
                // reemplaza al existente de ese mismo tipo en vez de convivir con él.
                var nuevoTipoId = tipoIds.GetValueOrDefault(valorCategoriaId);
                var idConflicto = nuevoTipoId.HasValue
                    ? asociados.FirstOrDefault(id => id != valorCategoriaId && tipoIds.GetValueOrDefault(id) == nuevoTipoId, 0)
                    : 0;

                if (idConflicto != 0)
                {
                    asociados.Remove(idConflicto);
                    var existente = await session.Query<ValorCategoriaConfiguradoEntity>()
                        .FirstOrDefaultAsync(v => v.ConfiguracionNomencladorId == configuracionId && v.ValorCategoriaId == idConflicto);
                    if (existente is not null)
                        await session.DeleteAsync(existente);
                }

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

    // Devuelve, para cada IdValorCategoria, el Id de su tipo (null si no tiene tipo asignado).
    private async Task<Dictionary<int, int?>> GetValorCategoriaTipoIdsAsync(IEnumerable<int> valorCategoriaIds)
    {
        var ids = valorCategoriaIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var result = new Dictionary<int, int?>();
        foreach (var chunk in GetChunks(ids))
        {
            var rows = await session.Query<ValorCategoriaCatalogEntity>()
                .Where(x => chunk.Contains(x.Id))
                .Select(x => new { x.Id, TipoId = x.Tipo == null ? (int?)null : x.Tipo.Id })
                .ToListAsync();

            foreach (var row in rows)
                result[row.Id] = row.TipoId;
        }

        return result;
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

    // NHibernate LINQ explota con "Evaluation failure on op_Implicit(...Int32[]...)" cuando
    // .Contains() se llama sobre un array (int[]) dentro de un .Where(); List<int> sí funciona.
    // Por eso los chunks se devuelven como List<int>, no int[] (ver también CatalogRepository,
    // que usa el mismo patrón .ToList() para los mismos fines).
    private static IEnumerable<List<int>> GetChunks(IReadOnlyCollection<int> ids)
    {
        var distinctIds = ids.Distinct().ToList();
        for (var i = 0; i < distinctIds.Count; i += OracleInClauseChunkSize)
        {
            yield return distinctIds.Skip(i).Take(OracleInClauseChunkSize).ToList();
        }
    }

    public async Task<int> AsociarConceptosMasivoAsync(IReadOnlyCollection<int> configuracionesIds, IReadOnlyCollection<int> conceptosIds)
    {
        if (configuracionesIds.Count == 0 || conceptosIds.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var existentesSet = new HashSet<(int ConfiguracionNomencladorId, int ConceptoId)>();
        foreach (var configuracionesChunk in GetChunks(configuracionesIds))
        {
            foreach (var conceptosChunk in GetChunks(conceptosIds))
            {
                var existentesChunk = await session.Query<ConceptoConfiguradoEntity>()
                    .Where(v => configuracionesChunk.Contains(v.ConfiguracionNomencladorId) && conceptosChunk.Contains(v.ConceptoId))
                    .Select(v => new { v.ConfiguracionNomencladorId, v.ConceptoId })
                    .ToListAsync();

                foreach (var e in existentesChunk)
                {
                    existentesSet.Add((e.ConfiguracionNomencladorId, e.ConceptoId));
                }
            }
        }
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

    // EscalaSalarialId es un FK simple (no una colección M:N), por eso "asociar" acá es un
    // UPDATE directo. Usado por la actualización masiva de escala salarial junto con
    // CatalogRepository.CloneEscalasMasivoAsync (clonar la escala primero, reasignar después).
    public async Task<Dictionary<int, int>> GetEscalaSalarialIdsAsync(IReadOnlyCollection<int> configuracionIds)
    {
        var ids = configuracionIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var result = new Dictionary<int, int>();
        foreach (var chunk in GetChunks(ids))
        {
            // Se carga la entidad completa en vez de proyectar a un tipo anónimo: algunas filas
            // tienen IDESCALASAL NULL en Oracle, y el transformer de tuplas de NHibernate para
            // proyecciones LINQ ("new { x.Id, x.EscalaSalarialId }") explota con
            // NullReferenceException al unboxear ese NULL, mientras que la carga de entidad
            // completa lo maneja bien (lo mapea a 0, igual que GetByIdAsync).
            var rows = await session.Query<ConfiguracionNomencladorEntity>()
                .Where(x => chunk.Contains(x.Id))
                .ToListAsync();

            foreach (var row in rows)
                result[row.Id] = row.EscalaSalarialId;
        }

        return result;
    }

    public async Task<int> ActualizarEscalaSalarialMasivoAsync(IReadOnlyDictionary<int, int> nuevaEscalaPorConfiguracion)
    {
        if (nuevaEscalaPorConfiguracion.Count == 0) return 0;

        using var tx = session.BeginTransaction();

        var actualizadas = 0;
        foreach (var (configuracionId, nuevaEscalaId) in nuevaEscalaPorConfiguracion)
        {
            var entity = await session.GetAsync<ConfiguracionNomencladorEntity>(configuracionId);
            if (entity is null) continue;

            entity.EscalaSalarialId = nuevaEscalaId;
            actualizadas++;
        }

        await session.FlushAsync();
        await tx.CommitAsync();

        return actualizadas;
    }
}
