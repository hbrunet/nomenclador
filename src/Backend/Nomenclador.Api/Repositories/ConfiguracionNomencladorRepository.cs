using Microsoft.EntityFrameworkCore;
using Nomenclador.Api.Data;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class ConfiguracionNomencladorRepository(NomencladorDbContext dbContext)
{
    public async Task<IReadOnlyCollection<ConfiguracionNomencladorEntity>> GetAllAsync(
        int? nomencladorId,
        int? escalaSalarialId,
        int? zonaId,
        DateOnly? vigenteEn)
    {
        var query = IncludeChildren();

        if (nomencladorId.HasValue)
        {
            query = query.Where(item => item.NomencladorId == nomencladorId.Value);
        }

        if (escalaSalarialId.HasValue)
        {
            query = query.Where(item => item.EscalaSalarialId == escalaSalarialId.Value);
        }

        if (zonaId.HasValue)
        {
            query = query.Where(item => item.ZonaId == zonaId.Value);
        }

        if (vigenteEn.HasValue)
        {
            query = query.Where(item => item.FechaInicio <= vigenteEn.Value &&
                                        (!item.FechaFin.HasValue || item.FechaFin.Value >= vigenteEn.Value));
        }

        return await query
            .OrderByDescending(item => item.FechaInicio)
            .ToListAsync();
    }

    public async Task<ConfiguracionNomencladorEntity?> GetByIdAsync(int id)
    {
        return await IncludeChildren().SingleOrDefaultAsync(item => item.Id == id);
    }

    public async Task AddAsync(ConfiguracionNomencladorEntity entity)
    {
        dbContext.Configuraciones.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> HasOverlapAsync(ConfiguracionNomencladorEntity entity, int? excludedId)
    {
        return await dbContext.Configuraciones
            .Where(item => item.Id != excludedId)
            .Where(item =>
                item.NomencladorId == entity.NomencladorId &&
                item.EscalaSalarialId == entity.EscalaSalarialId &&
                item.ZonaId == entity.ZonaId)
            .AnyAsync(item => RangesOverlap(item.FechaInicio, item.FechaFin, entity.FechaInicio, entity.FechaFin));
    }

    private IQueryable<ConfiguracionNomencladorEntity> IncludeChildren()
    {
        return dbContext.Configuraciones
            .Include(item => item.Conceptos)
            .Include(item => item.ValoresFijos)
            .Include(item => item.ValoresCategorias);
    }

    private static bool RangesOverlap(DateOnly firstStart, DateOnly? firstEnd, DateOnly secondStart, DateOnly? secondEnd)
    {
        var normalizedFirstEnd = firstEnd ?? DateOnly.MaxValue;
        var normalizedSecondEnd = secondEnd ?? DateOnly.MaxValue;

        return firstStart <= normalizedSecondEnd && secondStart <= normalizedFirstEnd;
    }
}
