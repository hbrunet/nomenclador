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
        var catalogs = await catalogRepository.GetSnapshotForListAsync();

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

        // Primero se guarda la entidad solo con los campos escalares para que la secuencia
        // le asigne un Id (entity.Id queda disponible de inmediato tras SaveAsync). Recién
        // entonces se pueden armar Conceptos/ValoresFijos/ValoresCategorias, cuya clave
        // compuesta requiere ese Id (ver ConfiguracionNomencladorMapper.ApplyChildren).
        var entity = mapper.ToNewEntity(request);
        await configuracionRepository.AddAsync(entity);

        mapper.ApplyChildren(entity, request);
        await configuracionRepository.SaveChangesAsync();

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

    public async Task<ConfiguracionNomencladorDetailDto> AddConceptoAsync(int id, ConceptoConfiguradoInputDto concepto)
    {
        if (await configuracionRepository.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"No se encontró la configuración {id}.");
        await configuracionRepository.AddConceptoAsync(id, concepto);

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
        var catalogs = await catalogRepository.GetSnapshotForEntityAsync(entity);
        return mapper.ToDetailDto(entity, catalogs);
    }

    public async Task<ConfiguracionNomencladorDetailDto> AddValorFijoAsync(int id, ValorFijoConfiguradoInputDto valorFijo)
    {
        if (await configuracionRepository.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"No se encontró la configuración {id}.");
        await configuracionRepository.AddValorFijoAsync(id, valorFijo);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> RemoveValorFijoAsync(int id, int valorFijoId)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        await configuracionRepository.RemoveValorFijoAsync(id, valorFijoId);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> AddValorPorCategoriaAsync(int id, ValorCategoriaConfiguradoInputDto valorCategoria)
    {
        if (await configuracionRepository.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"No se encontró la configuración {id}.");
        await configuracionRepository.AddValorPorCategoriaAsync(id, valorCategoria);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }

    public async Task<ConfiguracionNomencladorDetailDto> RemoveValorPorCategoriaAsync(int id, int valorCategoriaId)
    {
        var entity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        await configuracionRepository.RemoveValorPorCategoriaAsync(id, valorCategoriaId);

        var updatedEntity = await configuracionRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No se encontró la configuración {id}.");

        return await BuildDetailAsync(updatedEntity);
    }
}
