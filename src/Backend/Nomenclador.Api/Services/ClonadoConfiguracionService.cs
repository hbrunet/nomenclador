using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

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
            FechaInicio = new DateOnly(request.FechaInicio.Year, request.FechaInicio.Month, 1),
            FechaFin = new DateOnly(request.FechaFin?.Year ?? 9999, request.FechaFin?.Month ?? 12, 1),
            Conceptos = request.CopiarConceptos
                ? source.Conceptos.Select(item => new ConceptoConfiguradoInputDto
                {
                    IdConcepto = item.IdConcepto,
                    Orden = item.Orden,
                }).ToList()
                : [],
            ValoresFijos = request.CopiarValoresFijos
                ? source.ValoresFijos.Select(item => new ValorFijoConfiguradoInputDto
                {
                    IdValorFijo = item.IdValorFijo,
                }).ToList()
                : [],
            ValoresCategorias = request.CopiarValoresCategoria
                ? source.ValoresCategorias.Select(item => new ValorCategoriaConfiguradoInputDto
                {
                    IdValorCategoria = item.IdValorCategoria,
                }).ToList()
                : []
        };
    }

    public ConfiguracionNomencladorCreateUpdateDto BuildClone(
        ConfiguracionNomencladorCloneSource source,
        ClonarConfiguracionDto request)
    {
        return new ConfiguracionNomencladorCreateUpdateDto
        {
            IdNomenclador = source.Entity.NomencladorId,
            IdEscalaSalarial = source.Entity.EscalaSalarialId,
            IdZona = source.Entity.ZonaId,
            FechaInicio = new DateOnly(request.FechaInicio.Year, request.FechaInicio.Month, 1),
            FechaFin = new DateOnly(request.FechaFin?.Year ?? 9999, request.FechaFin?.Month ?? 12, 1),
            Conceptos = request.CopiarConceptos
                ? source.Conceptos.Select(item => new ConceptoConfiguradoInputDto
                {
                    IdConcepto = item.ConceptoId,
                    Orden = item.Orden,
                }).ToList()
                : [],
            ValoresFijos = request.CopiarValoresFijos
                ? source.ValoresFijos.Select(item => new ValorFijoConfiguradoInputDto
                {
                    IdValorFijo = item.ValorFijoId,
                }).ToList()
                : [],
            ValoresCategorias = request.CopiarValoresCategoria
                ? source.ValoresCategorias.Select(item => new ValorCategoriaConfiguradoInputDto
                {
                    IdValorCategoria = item.ValorCategoriaId,
                }).ToList()
                : []
        };
    }
}
