using NHibernate;
using NHibernate.Linq;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class ConfiguracionNomencladorRepository(NHibernate.ISession session)
{
    public async Task<IReadOnlyCollection<ConfiguracionNomencladorEntity>> GetAllAsync(
        int? nomencladorId,
        int? escalaSalarialId,
        int? zonaId,
        DateOnly? vigenteEn)
    {
        var query = session.Query<ConfiguracionNomencladorEntity>();

        if (nomencladorId.HasValue)
            query = query.Where(c => c.NomencladorId == nomencladorId.Value);

        if (escalaSalarialId.HasValue)
            query = query.Where(c => c.EscalaSalarialId == escalaSalarialId.Value);

        if (zonaId.HasValue)
            query = query.Where(c => c.ZonaId == zonaId.Value);

        var items = await query
            .OrderByDescending(c => c.FechaInicio)
            .ToListAsync();

        // El filtro de vigencia con DateOnly? se aplica en memoria para evitar
        // problemas de traducción SQL con tipos personalizados en Oracle 11g.
        if (vigenteEn.HasValue)
        {
            items = items
                .Where(c => c.FechaInicio <= vigenteEn.Value &&
                            (!c.FechaFin.HasValue || c.FechaFin.Value >= vigenteEn.Value))
                .ToList();
        }

        return items;
    }

    public async Task<ConfiguracionNomencladorEntity?> GetByIdAsync(int id)
    {
        return await session.GetAsync<ConfiguracionNomencladorEntity>(id);
    }

    public async Task AddAsync(ConfiguracionNomencladorEntity entity)
    {
        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
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

