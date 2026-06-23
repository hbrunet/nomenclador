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

        if (request.IdZona <= 0)
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "ZONA_REQUERIDA",
                Mensaje = "Debe seleccionar una zona.",
                Campo = "idZona"
            });
        }

        if (request.FechaFin.HasValue && request.FechaFin.Value < request.FechaInicio)
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "FECHA_INVALIDA",
                Mensaje = "La fecha fin no puede ser menor a la fecha inicio.",
                Campo = "fechaFin"
            });
        }

        if (!request.Conceptos.Any())
        {
            warnings.Add(new ValidationMessageDto
            {
                Codigo = "SIN_CONCEPTOS",
                Mensaje = "La configuración todavía no tiene conceptos asociados."
            });
        }

        if (request.Conceptos.GroupBy(item => item.IdConcepto).Any(group => group.Count() > 1))
        {
            warnings.Add(new ValidationMessageDto
            {
                Codigo = "CONCEPTOS_DUPLICADOS",
                Mensaje = "Hay conceptos repetidos en la configuración."
            });
        }

        if (request.ValoresFijos.GroupBy(item => item.IdValorFijo).Any(group => group.Count() > 1))
        {
            warnings.Add(new ValidationMessageDto
            {
                Codigo = "VALORES_FIJOS_DUPLICADOS",
                Mensaje = "Hay valores fijos repetidos en la configuración."
            });
        }

        if (request.ValoresCategorias.GroupBy(item => item.IdValorCategoria).Any(group => group.Count() > 1))
        {
            warnings.Add(new ValidationMessageDto
            {
                Codigo = "CATEGORIAS_DUPLICADAS",
                Mensaje = "Hay categorías repetidas en la grilla de valores."
            });
        }

        var entity = mapper.ToNewEntity(request);
        if (await configuracionRepository.HasOverlapAsync(entity, excludedId))
        {
            errores.Add(new ValidationMessageDto
            {
                Codigo = "VIGENCIA_SUPERPUESTA",
                Mensaje = "Ya existe una configuración activa para el mismo nomenclador, escala y zona en ese rango de fechas.",
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
