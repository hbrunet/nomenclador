using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class ConfiguracionNomencladorRepository(NHibernate.ISession session)
{
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
        var query = session.QueryOver(() => alias);

        if (nomencladorId.HasValue)
            query.Where(() => alias.NomencladorId == nomencladorId.Value);

        if (escalaSalarialId.HasValue)
            query.Where(() => alias.EscalaSalarialId == escalaSalarialId.Value);

        if (zonaId.HasValue)
            query.Where(() => alias.ZonaId == zonaId.Value);

        if (vigenteEn.HasValue)
        {
            var fecha = vigenteEn.Value;
            // Usar Restrictions para que la comparación de DateOnly? se traduzca
            // correctamente a SQL en Oracle 11g sin pasar por el filtro en memoria.
            query.Where(Restrictions.Le(
                Projections.Property(() => alias.FechaInicio), fecha));
            query.Where(Restrictions.Or(
                Restrictions.IsNull(Projections.Property(() => alias.FechaFin)),
                Restrictions.Ge(Projections.Property(() => alias.FechaFin), fecha)));
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
                    query.Where(Restrictions.Or(
                        Restrictions.IsNull(Projections.Property(() => alias.FechaFin)),
                        Restrictions.Ge(Projections.Property(() => alias.FechaFin), today)));
                    break;
            }
        }

        var total = await query.RowCountAsync();

        var rawItems = await query
            .OrderBy(() => alias.FechaInicio).Desc
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

    public async Task AddAsync(ConfiguracionNomencladorEntity entity)
    {
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await tx.CommitAsync();
    }

    public async Task AddConceptoAsync(int configuracionId, Nomenclador.Api.DTOs.ConceptoConfiguradoInputDto request)
    {
        using var tx = session.BeginTransaction();

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

        var entity = new ValorFijoConfiguradoEntity
        {
            ConfiguracionNomencladorId = configuracionId,
            ValorFijoId = request.IdValorFijo,
        };

        await session.SaveAsync(entity);
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

    public async Task SaveChangesAsync()
    {
        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();
    }

    public async Task<bool> HasOverlapAsync(ConfiguracionNomencladorEntity entity, int? excludedId)
    {
        // Se cargan los candidatos por nomenclador/escala/zona y el solapamiento
        // de fechas se verifica en memoria (los registros por combinación son pocos).
        var candidatos = await session.Query<ConfiguracionNomencladorEntity>()
            .Where(c => c.NomencladorId == entity.NomencladorId)
            .Where(c => c.EscalaSalarialId == entity.EscalaSalarialId)
            .Where(c => c.ZonaId == entity.ZonaId)
            .Where(c => !excludedId.HasValue || c.Id != excludedId.Value)
            .ToListAsync();

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
}

