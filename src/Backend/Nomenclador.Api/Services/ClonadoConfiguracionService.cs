using Nomenclador.Api.DTOs;

namespace Nomenclador.Api.Services;

public sealed class ClonadoConfiguracionService
{
    public ConfiguracionNomencladorCreateUpdateDto BuildClone(
        ConfiguracionNomencladorDetailDto source,
        ClonarConfiguracionDto request)
    {
        return new ConfiguracionNomencladorCreateUpdateDto
        {
            IdNomenclador = source.IdNomenclador,
            IdEscalaSalarial = source.IdEscalaSalarial,
            IdZona = source.IdZona,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Conceptos = request.CopiarConceptos
                ? source.Conceptos.Select(item => new ConceptoConfiguradoInputDto
                {
                    IdConcepto = item.IdConcepto,
                    Orden = item.Orden,
                    Activo = item.Activo
                }).ToList()
                : [],
            ValoresFijos = request.CopiarValoresFijos
                ? source.ValoresFijos.Select(item => new ValorFijoConfiguradoInputDto
                {
                    IdValorFijo = item.IdValorFijo,
                    Importe = item.Importe
                }).ToList()
                : [],
            ValoresCategorias = request.CopiarValoresCategoria
                ? source.ValoresCategorias.Select(item => new ValorCategoriaConfiguradoInputDto
                {
                    IdCategoria = item.IdCategoria,
                    Importe = item.Importe
                }).ToList()
                : []
        };
    }
}
