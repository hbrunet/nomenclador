using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Mappers;

public sealed class ConfiguracionNomencladorMapper
{
    public ConfiguracionNomencladorEntity ToNewEntity(ConfiguracionNomencladorCreateUpdateDto dto)
    {
        return Apply(new ConfiguracionNomencladorEntity(), dto);
    }

    public ConfiguracionNomencladorEntity Apply(ConfiguracionNomencladorEntity entity, ConfiguracionNomencladorCreateUpdateDto dto)
    {
        entity.NomencladorId = dto.IdNomenclador;
        entity.EscalaSalarialId = dto.IdEscalaSalarial;
        entity.ZonaId = dto.IdZona;
        entity.FechaInicio = dto.FechaInicio;
        entity.FechaFin = dto.FechaFin;
        entity.Conceptos = dto.Conceptos
            .OrderBy(item => item.Orden)
            .Select(item => new ConceptoConfiguradoEntity
            {
                ConceptoId = item.IdConcepto,
                Orden = item.Orden,
            })
            .ToList();
        entity.ValoresFijos = dto.ValoresFijos
            .Select(item => new ValorFijoConfiguradoEntity
            {
                ValorFijoId = item.IdValorFijo,
            })
            .ToList();
        entity.ValoresCategorias = dto.ValoresCategorias
            .Select(item => new ValorCategoriaConfiguradoEntity
            {
                ValorCategoriaId = item.IdValorCategoria,
            })
            .ToList();

        return entity;
    }

    public ConfiguracionNomencladorViewModel ToViewModel(ConfiguracionNomencladorEntity entity, CatalogSnapshot catalogs)
    {
        return new ConfiguracionNomencladorViewModel
        {
            Id = entity.Id,
            Nomenclador = MapCatalogItem(catalogs.Nomencladores, entity.NomencladorId),
            EscalaSalarial = MapCatalogItem(catalogs.EscalasSalariales, entity.EscalaSalarialId),
            Zona = MapCatalogItem(catalogs.Zonas, entity.ZonaId),
            FechaInicio = entity.FechaInicio,
            FechaFin = entity.FechaFin,
            Estado = ResolveEstado(entity.FechaInicio, entity.FechaFin),
            Conceptos = entity.Conceptos
                .OrderBy(item => item.Orden)
                .Select(item =>
                {
                    var concepto = catalogs.Conceptos[item.ConceptoId];
                    return new ConceptoConfiguradoViewModel
                    {
                        IdConcepto = concepto.Id,
                        Codigo = concepto.Codigo,
                        Subcodigo = concepto.Subcodigo,
                        Descripcion = concepto.Descripcion,
                        Orden = item.Orden,
                    };
                })
                .ToList(),
            ValoresFijos = entity.ValoresFijos
                .Select(item =>
                {
                    var valorFijo = catalogs.ValoresFijos[item.ValorFijoId];
                    return new ValorFijoConfiguradoViewModel
                    {
                        IdValorFijo = valorFijo.Id,
                        Descripcion = valorFijo.Descripcion,
                        Tipo = valorFijo.Tipo?.Descripcion ?? string.Empty,
                    };
                })
                .ToList(),
            ValoresCategorias = entity.ValoresCategorias
                .Select(item =>
                {
                    var valorCategoria = catalogs.ValoresCategorias[item.ValorCategoriaId];
                    return new ValorCategoriaConfiguradoViewModel
                    {
                        IdValorCategoria = valorCategoria.Id,
                        Descripcion = valorCategoria.Descripcion,
                        Tipo = valorCategoria.Tipo?.Descripcion ?? string.Empty,
                    };
                })
                .ToList()
        };
    }

    public ConfiguracionNomencladorListItemDto ToListItemDto(ConfiguracionNomencladorViewModel viewModel)
    {
        return new ConfiguracionNomencladorListItemDto
        {
            Id = viewModel.Id,
            NomencladorDescripcion = viewModel.Nomenclador.Descripcion,
            EscalaDescripcion = viewModel.EscalaSalarial.Descripcion,
            ZonaDescripcion = viewModel.Zona.Descripcion,
            FechaInicio = viewModel.FechaInicio,
            FechaFin = viewModel.FechaFin,
            Estado = viewModel.Estado,
            CantidadConceptos = viewModel.Conceptos.Count,
            CantidadValoresFijos = viewModel.ValoresFijos.Count
        };
    }

    public ConfiguracionNomencladorDetailDto ToDetailDto(ConfiguracionNomencladorViewModel viewModel)
    {
        return new ConfiguracionNomencladorDetailDto
        {
            Id = viewModel.Id,
            IdNomenclador = viewModel.Nomenclador.Id,
            NomencladorDescripcion = viewModel.Nomenclador.Descripcion,
            IdEscalaSalarial = viewModel.EscalaSalarial.Id,
            EscalaDescripcion = viewModel.EscalaSalarial.Descripcion,
            IdZona = viewModel.Zona.Id,
            ZonaDescripcion = viewModel.Zona.Descripcion,
            FechaInicio = viewModel.FechaInicio,
            FechaFin = viewModel.FechaFin,
            Estado = viewModel.Estado,
            Conceptos = viewModel.Conceptos,
            ValoresFijos = viewModel.ValoresFijos,
            ValoresCategorias = viewModel.ValoresCategorias
        };
    }

    private static CatalogItemViewModel MapCatalogItem<TCatalog>(IReadOnlyDictionary<int, TCatalog> catalog, int id)
        where TCatalog : CatalogEntityBase
    {
        return new CatalogItemViewModel
        {
            Id = id,
            Descripcion = catalog.TryGetValue(id, out var item) ? item.Descripcion : "Sin catálogo"
        };
    }

    private static string ResolveEstado(DateOnly fechaInicio, DateOnly? fechaFin)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (fechaInicio > today)
        {
            return "Futura";
        }

        if (fechaFin.HasValue && fechaFin.Value < today)
        {
            return "Vencida";
        }

        return "Activa";
    }
}
