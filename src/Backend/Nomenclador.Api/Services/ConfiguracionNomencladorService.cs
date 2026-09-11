using System.Data;
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
        var catalogs = await catalogRepository.GetSnapshotForListAsync();
        var periodoActivo = await catalogRepository.GetPeriodoActivoAsync();
        var (entities, total) = await configuracionRepository.GetAllAsync(
            nomencladorId, escalaSalarialId, zonaId, vigenteEn, estado, page, pageSize, periodoActivo);

        var items = entities
            .Select(entity => mapper.ToListItemDto(entity, catalogs, periodoActivo))
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
        await configuracionRepository.AddAsync(entity, e => mapper.ApplyChildren(e, request));

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

    // Clona en lote N configuraciones activas a un nuevo período y cierra cada una de las
    // originales (FechaFin = mes anterior al nuevo período), aplicando las mismas reglas de la
    // clonación individual (overlap, copiarConceptos/ValoresFijos/ValoresCategoria).
    public async Task<ClonacionMasivaConfiguracionesResultDto> ClonarMasivoAsync(ClonacionMasivaConfiguracionesDto request)
    {
        var nuevaFechaInicio = new DateOnly(request.FechaInicio.Year, request.FechaInicio.Month, 1);
        var requestedIds = request.ConfiguracionesIds.Distinct().ToList();
        var catalogs = await catalogRepository.GetSnapshotForListAsync();
        var periodoActivo = await catalogRepository.GetPeriodoActivoAsync();

        var fechaFinCierre = nuevaFechaInicio.AddMonths(-1);
        var clonarRequest = new ClonarConfiguracionDto
        {
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            CopiarConceptos = request.CopiarConceptos,
            CopiarValoresFijos = request.CopiarValoresFijos,
            CopiarValoresCategoria = request.CopiarValoresCategoria,
        };

        var cloneEntities = new List<ConfiguracionNomencladorEntity>();
        var configuracionesCerradas = 0;
        await configuracionRepository.ExecuteInTransactionAsync(async () =>
        {
            var loadedSources = await configuracionRepository.GetCloneSourcesByIdsAsync(requestedIds, lockEntities: true);
            var sourcesById = loadedSources.ToDictionary(source => source.Entity.Id);
            var sources = new List<ConfiguracionNomencladorCloneSource>();
            var errores = new List<ValidationMessageDto>();

            foreach (var id in requestedIds)
            {
                if (!sourcesById.TryGetValue(id, out var source))
                    throw new KeyNotFoundException($"No se encontró la configuración {id}.");

                var sourceListItem = mapper.ToListItemDto(source.Entity, catalogs, periodoActivo);
                if (sourceListItem.Estado != "Activa")
                {
                    errores.Add(new ValidationMessageDto
                    {
                        Codigo = "CONFIGURACION_NO_ACTIVA",
                        Mensaje = $"La configuración '{sourceListItem.NomencladorDescripcion} - {sourceListItem.EscalaDescripcion}' no está activa.",
                        Campo = "configuracionesIds",
                    });
                    continue;
                }

                if (nuevaFechaInicio <= source.Entity.FechaInicio)
                {
                    errores.Add(new ValidationMessageDto
                    {
                        Codigo = "PERIODO_CLONACION_INVALIDO",
                        Mensaje = $"El período de clonación ({nuevaFechaInicio:MM/yyyy}) debe ser posterior al de la configuración '{sourceListItem.NomencladorDescripcion} - {sourceListItem.EscalaDescripcion}' (vigente desde {source.Entity.FechaInicio:MM/yyyy}).",
                        Campo = "fechaInicio",
                    });
                    continue;
                }

                sources.Add(source);
            }

            if (errores.Count > 0)
            {
                throw new ConfiguracionValidationException(new ValidacionConfiguracionResponse
                {
                    Valida = false,
                    Errores = errores,
                });
            }

            var cloneRequests = new List<(ConfiguracionNomencladorCloneSource Source, ConfiguracionNomencladorCreateUpdateDto CloneRequest)>();
            foreach (var source in sources)
            {
                var cloneRequest = clonadoConfiguracionService.BuildClone(source, clonarRequest);
                cloneRequests.Add((source, cloneRequest));
            }

            var duplicateKeys = cloneRequests
                .GroupBy(item => (
                    item.CloneRequest.IdNomenclador,
                    item.CloneRequest.IdEscalaSalarial,
                    item.CloneRequest.IdZona))
                .Where(group => group.Count() > 1)
                .ToList();

            if (duplicateKeys.Count > 0)
            {
                throw new ConfiguracionValidationException(new ValidacionConfiguracionResponse
                {
                    Valida = false,
                    Errores =
                    [
                        new ValidationMessageDto
                        {
                            Codigo = "VIGENCIA_SUPERPUESTA",
                            Mensaje = "La clonación masiva genera configuraciones superpuestas para el mismo nomenclador, escala y zona.",
                            Campo = "configuracionesIds",
                        },
                    ],
                });
            }

            var bulkValidation = await validacionService.ValidateBulkCloneAsync(
                cloneRequests.Select(item => item.CloneRequest).ToList(),
                sources.Select(source => source.Entity.Id).ToList());
            if (!bulkValidation.Valida)
            {
                throw new ConfiguracionValidationException(bulkValidation);
            }

            foreach (var (source, cloneRequest) in cloneRequests)
            {
                source.Entity.FechaFin = fechaFinCierre;

                var cloneEntity = mapper.ToNewEntity(cloneRequest);
                await configuracionRepository.AddAsync(cloneEntity, e => mapper.ApplyChildren(e, cloneRequest), useTransaction: false);
                cloneEntities.Add(cloneEntity);
            }

            configuracionesCerradas = sources.Count;
        }, IsolationLevel.Serializable);

        await configuracionRepository.LoadValorCategoriaItemsAsync(cloneEntities);
        var cloneCatalogs = await catalogRepository.GetSnapshotForEntitiesAsync(cloneEntities);
        var clones = cloneEntities
            .Select(entity => mapper.ToDetailDto(entity, cloneCatalogs, periodoActivo))
            .ToList();

        return new ClonacionMasivaConfiguracionesResultDto
        {
            Clones = clones,
            ConfiguracionesCerradas = configuracionesCerradas,
        };
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
        var periodoActivo = await catalogRepository.GetPeriodoActivoAsync();
        return mapper.ToDetailDto(entity, catalogs, periodoActivo);
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

    // Clona cada escala salarial distinta referenciada por las configuraciones seleccionadas
    // (aplicando el coeficiente al Monto de sus categorías) y reasigna cada configuración a
    // su propio clon. No valida solapamiento de fechas al reasignar (decisión de negocio).
    // Si alguna escala ya tiene un clon para el período pedido y !ActualizarSiExiste, aborta
    // sin mutar nada y devuelve los conflictos (con las configuraciones que la referencian)
    // para que el usuario decida: deseleccionarlas o reintentar con ActualizarSiExiste.
    public async Task<ActualizacionMasivaEscalaSalarialResponseDto> ActualizarEscalaSalarialMasivoAsync(
        ActualizacionMasivaEscalaSalarialDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToList();
        if (configuracionesIds.Count == 0)
            return new ActualizacionMasivaEscalaSalarialResponseDto { Resultado = new() };

        var escalaIdPorConfiguracion = await configuracionRepository.GetEscalaSalarialIdsAsync(configuracionesIds);
        var escalaIdsDistintas = escalaIdPorConfiguracion.Values.Where(v => v > 0).Distinct().ToList();

        if (!request.ActualizarSiExiste)
        {
            var conflictosDetectados = await catalogRepository.DetectarConflictosClonEscalasAsync(
                escalaIdsDistintas, request.NuevoPeriodo);

            if (conflictosDetectados.Count > 0)
            {
                var conflictos = conflictosDetectados.Select(c => new EscalaCloneConflictDto
                {
                    EscalaOriginalId = c.EscalaOriginalId,
                    EscalaOriginalDescripcion = c.EscalaOriginalDescripcion,
                    EscalaExistenteId = c.EscalaExistenteId,
                    EscalaExistenteDescripcion = c.EscalaExistenteDescripcion,
                    ConfiguracionesIds = escalaIdPorConfiguracion
                        .Where(kv => kv.Value == c.EscalaOriginalId)
                        .Select(kv => kv.Key)
                        .ToList(),
                }).ToList();

                return new ActualizacionMasivaEscalaSalarialResponseDto { Conflictos = conflictos };
            }
        }

        var nuevaEscalaPorOriginal = await catalogRepository.CloneOActualizarEscalasMasivoAsync(
            escalaIdsDistintas, request.NuevoPeriodo, request.CoeficienteAjuste, request.ActualizarSiExiste);

        var nuevaEscalaPorConfiguracion = new Dictionary<int, int>();
        foreach (var (configuracionId, escalaOriginalId) in escalaIdPorConfiguracion)
        {
            if (escalaOriginalId <= 0) continue;
            if (!nuevaEscalaPorOriginal.TryGetValue(escalaOriginalId, out var nuevaEscalaId)) continue;

            nuevaEscalaPorConfiguracion[configuracionId] = nuevaEscalaId;
        }

        var actualizadas = await configuracionRepository.ActualizarEscalaSalarialMasivoAsync(nuevaEscalaPorConfiguracion);

        return new ActualizacionMasivaEscalaSalarialResponseDto
        {
            Resultado = new ActualizacionMasivaEscalaSalarialResultDto
            {
                EscalasClonadas = nuevaEscalaPorOriginal.Count,
                ConfiguracionesActualizadas = actualizadas,
            },
        };
    }

    public async Task<AsociacionMasivaResultDto> AsociarValoresFijosMasivoAsync(AsociacionMasivaValoresFijosDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToArray();
        var valoresFijosIds = request.ValoresFijosIds.Distinct().ToArray();

        var creadas = await configuracionRepository.AsociarValoresFijosMasivoAsync(
            configuracionesIds, valoresFijosIds);

        var total = configuracionesIds.Length * valoresFijosIds.Length;

        return new AsociacionMasivaResultDto
        {
            AsociacionesCreadas = creadas,
            AsociacionesExistentes = total - creadas,
        };
    }

    public async Task<DesasociacionMasivaResultDto> DesasociarValoresFijosMasivoAsync(AsociacionMasivaValoresFijosDto request)
    {
        var eliminadas = await configuracionRepository.DesasociarValoresFijosMasivoAsync(
            request.ConfiguracionesIds, request.ValoresFijosIds);

        var total = request.ConfiguracionesIds.Count * request.ValoresFijosIds.Count;

        return new DesasociacionMasivaResultDto
        {
            AsociacionesEliminadas = eliminadas,
            AsociacionesInexistentes = total - eliminadas,
        };
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

    public async Task<AsociacionMasivaResultDto> AsociarValoresCategoriasMasivoAsync(AsociacionMasivaValoresCategoriasDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToArray();
        var valoresCategoriasIds = request.ValoresCategoriasIds.Distinct().ToArray();

        var creadas = await configuracionRepository.AsociarValoresCategoriasMasivoAsync(
            configuracionesIds, valoresCategoriasIds);

        var total = configuracionesIds.Length * valoresCategoriasIds.Length;

        return new AsociacionMasivaResultDto
        {
            AsociacionesCreadas = creadas,
            AsociacionesExistentes = total - creadas,
        };
    }

    public async Task<DesasociacionMasivaResultDto> DesasociarValoresCategoriasMasivoAsync(AsociacionMasivaValoresCategoriasDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToArray();
        var valoresCategoriasIds = request.ValoresCategoriasIds.Distinct().ToArray();

        var eliminadas = await configuracionRepository.DesasociarValoresCategoriasMasivoAsync(
            configuracionesIds, valoresCategoriasIds);

        var total = configuracionesIds.Length * valoresCategoriasIds.Length;

        return new DesasociacionMasivaResultDto
        {
            AsociacionesEliminadas = eliminadas,
            AsociacionesInexistentes = total - eliminadas,
        };
    }

    public async Task<AsociacionMasivaResultDto> AsociarConceptosMasivoAsync(AsociacionMasivaConceptosDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToArray();
        var conceptosIds = request.ConceptosIds.Distinct().ToArray();

        var creadas = await configuracionRepository.AsociarConceptosMasivoAsync(
            configuracionesIds, conceptosIds);

        var total = configuracionesIds.Length * conceptosIds.Length;

        return new AsociacionMasivaResultDto
        {
            AsociacionesCreadas = creadas,
            AsociacionesExistentes = total - creadas,
        };
    }

    public async Task<DesasociacionMasivaResultDto> DesasociarConceptosMasivoAsync(AsociacionMasivaConceptosDto request)
    {
        var configuracionesIds = request.ConfiguracionesIds.Distinct().ToArray();
        var conceptosIds = request.ConceptosIds.Distinct().ToArray();

        var eliminadas = await configuracionRepository.DesasociarConceptosMasivoAsync(
            configuracionesIds, conceptosIds);

        var total = configuracionesIds.Length * conceptosIds.Length;

        return new DesasociacionMasivaResultDto
        {
            AsociacionesEliminadas = eliminadas,
            AsociacionesInexistentes = total - eliminadas,
        };
    }
}
