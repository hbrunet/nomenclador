using Nomenclador.Api.DTOs;
using Nomenclador.Api.Mappers;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Services;

public sealed class ValidacionConfiguracionService(
    ConfiguracionNomencladorRepository configuracionRepository,
    ConfiguracionNomencladorMapper mapper)
{
    public async Task<ValidacionConfiguracionResponse> ValidateAsync(ConfiguracionNomencladorCreateUpdateDto request, int? excludedId)
    {
        var errores = new List<ValidationMessageDto>();
        var warnings = new List<ValidationMessageDto>();

        AddBasicErrors(request, errores);

        var entity = mapper.ToNewEntity(request);
        if (await configuracionRepository.HasOverlapAsync(entity, excludedId))
        {
            errores.Add(CreateOverlapError());
        }

        return new ValidacionConfiguracionResponse
        {
            Valida = errores.Count == 0,
            Errores = errores,
            Warnings = warnings
        };
    }

    public async Task<ValidacionConfiguracionResponse> ValidateBulkCloneAsync(
        IReadOnlyCollection<ConfiguracionNomencladorCreateUpdateDto> requests,
        IReadOnlyCollection<int> sourceIds)
    {
        var errores = new List<ValidationMessageDto>();

        foreach (var request in requests)
        {
            AddBasicErrors(request, errores);
        }

        var entities = requests.Select(mapper.ToNewEntity).ToList();
        if (errores.Count == 0 && await configuracionRepository.HasAnyOverlapAsync(entities, sourceIds))
        {
            errores.Add(CreateOverlapError());
        }

        return new ValidacionConfiguracionResponse
        {
            Valida = errores.Count == 0,
            Errores = errores,
            Warnings = [],
        };
    }

    private static void AddBasicErrors(
        ConfiguracionNomencladorCreateUpdateDto request,
        ICollection<ValidationMessageDto> errores)
    {

        if (request.IdNomenclador <= 0)
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "NOMENCLADOR_REQUERIDO",
                Mensaje = "Debe seleccionar un nomenclador.",
                Campo = "idNomenclador"
            });
        }

        if (request.IdEscalaSalarial <= 0)
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "ESCALA_REQUERIDA",
                Mensaje = "Debe seleccionar una escala salarial.",
                Campo = "idEscalaSalarial"
            });
        }

        if (request.FechaFin.HasValue && request.FechaFin.Value < request.FechaInicio)
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "FECHA_FIN_INVALIDA",
                Mensaje = "La fecha fin no puede ser menor a la fecha inicio.",
                Campo = "fechaFin"
            });
        }
    }

    private static ValidationMessageDto CreateOverlapError()
    {
        return new ValidationMessageDto
        {
            Codigo = "VIGENCIA_SUPERPUESTA",
            Mensaje = "Ya existe una configuración para el mismo nomenclador en ese rango de fechas.",
            Campo = "fechaInicio"
        };
    }
}
