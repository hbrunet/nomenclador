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

        var entity = mapper.ToNewEntity(request);
        if (await configuracionRepository.HasOverlapAsync(entity, excludedId))
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "VIGENCIA_SUPERPUESTA",
                Mensaje = "Ya existe una configuración para el mismo nomenclador en ese rango de fechas.",
                Campo = "fechaInicio"
            });
        }

        return new ValidacionConfiguracionResponse
        {
            Valida = errores.Count == 0,
            Errores = errores,
            Warnings = warnings
        };
    }
}
