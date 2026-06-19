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
    public async Task<IReadOnlyCollection<ConfiguracionNomencladorListItemDto>> GetAllAsync(
        int? nomencladorId,
        int? escalaSalarialId,
        int? zonaId,
        DateOnly? vigenteEn,
        string? estado)
    {
        var entities = await configuracionRepository.GetAllAsync(nomencladorId, escalaSalarialId, zonaId, vigenteEn);
        var catalogs = await catalogRepository.GetSnapshotAsync();

        var items = entities
            .Select(entity => mapper.ToListItemDto(mapper.ToViewModel(entity, catalogs)));

        if (string.IsNullOrWhiteSpace(estado))
        {
            return items.ToList();
        }

        return items
            .Where(item => item.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase))
            .ToList();
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
        var viewModel = mapper.ToViewModel(entity, catalogs);
        return mapper.ToDetailDto(viewModel);
    }
}
