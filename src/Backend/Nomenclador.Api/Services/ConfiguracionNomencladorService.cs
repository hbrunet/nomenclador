using Nomenclador.Api.DTOs;
using Nomenclador.Api.Mappers;
using Nomenclador.Api.Models;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Services;

public sealed class ConfiguracionNomencladorService(
    ConfiguracionNomencladorRepository configuracionRepository,
    CatalogRepository catalogRepository,
    ValidacionConfiguracionService validacionService,
    ConfiguracionNomencladorMapper mapper,
    ClonadoConfiguracionService clonadoConfiguracionService)
{
    public async Task<PagedResult<ConfiguracionNomencladorListItemDto>> GetAllAsync(
        int? nomencladorId,
        int? escalaSalarialId,
        int? zonaId,
        DateOnly? vigenteEn,
        string? estado,
        int page = 1,
        int pageSize = 20)
    {
        var (entities, total) = await configuracionRepository.GetAllAsync(
            nomencladorId, escalaSalarialId, zonaId, vigenteEn, estado, page, pageSize);
        var catalogs = await catalogRepository.GetSnapshotAsync();

        var items = entities
            .Select(entity => mapper.ToListItemDto(entity, catalogs))
            .ToList();

        return new PagedResult<ConfiguracionNomencladorListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ConfiguracionNomencladorDetailDto> GetByIdAsync(int id)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(entity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> CreateAsync(ConfiguracionNomencladorCreateUpdateDto request)
    {
        await EnsureValidAsync(request, null);

        var entity = mapper.ToNewEntity(request);
        await configuracionRepository.AddAsync(entity);

        var createdEntity = await configuracionRepository.GetByIdAsync(entity.Id)
            ?? throw new KeyNotFoundException("No se pudo recuperar la configuración creada.");

        return await BuildDetailAsync(createdEntity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> UpdateAsync(int id, ConfiguracionNomencladorCreateUpdateDto request)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        await EnsureValidAsync(request, id);

        mapper.Apply(entity, request);
        await configuracionRepository.SaveChangesAsync();

        return await BuildDetailAsync(entity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> AddConceptosAsync(int id, IReadOnlyCollection<ConceptoConfiguradoInputDto> conceptos)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        await configuracionRepository.AddConceptosAsync(id, conceptos);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> RemoveConceptoAsync(int id, int conceptoId)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        await configuracionRepository.RemoveConceptoAsync(id, conceptoId);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }

    public Task<ValidacionConfiguracionResponse> ValidateAsync(ConfiguracionNomencladorCreateUpdateDto request, int? excludedId = null)
    {
        return validacionService.ValidateAsync(request, excludedId);
    }

    public async Task<ConfiguracionNomencladorDetailDto> CloneAsync(int sourceId, ClonarConfiguracionDto request)
    {
        var source = await GetByIdAsync(sourceId);
        var cloneRequest = clonadoConfiguracionService.BuildClone(source, request);
        return await CreateAsync(cloneRequest);
    }

    private async Task EnsureValidAsync(ConfiguracionNomencladorCreateUpdateDto request, int? excludedId)
    {
        var validation = await validacionService.ValidateAsync(request, excludedId);

        if (!validation.Valida)
        {
            throw new ConfiguracionValidationException(validation);
        }
    }

    private async Task<ConfiguracionNomencladorDetailDto> BuildDetailAsync(ConfiguracionNomencladorEntity entity)
    {
        var catalogs = await catalogRepository.GetSnapshotAsync();
        return mapper.ToDetailDto(entity, catalogs);
    }
}
