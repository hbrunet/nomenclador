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
                Id = 0,
                ConceptoId = item.IdConcepto,
                Orden = item.Orden,
                Activo = item.Activo
            })
            .ToList();
        entity.ValoresFijos = dto.ValoresFijos
            .Select(item => new ValorFijoConfiguradoEntity
            {
                Id = 0,
                ValorFijoId = item.IdValorFijo,
                Importe = item.Importe
            })
            .ToList();
        entity.ValoresCategorias = dto.ValoresCategorias
            .Select(item => new ValorCategoriaConfiguradoEntity
            {
                Id = 0,
                CategoriaId = item.IdCategoria,
                Importe = item.Importe
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
                        IdRelacion = item.Id,
                        IdConcepto = concepto.Id,
                        Codigo = concepto.Codigo,
                        Subcodigo = concepto.Subcodigo,
                        Descripcion = concepto.Descripcion,
                        Clasificacion = concepto.Clasificacion,
                        Orden = item.Orden,
                        Activo = item.Activo
                    };
                })
                .ToList(),
            ValoresFijos = entity.ValoresFijos
                .Select(item =>
                {
                    var valorFijo = catalogs.ValoresFijos[item.ValorFijoId];
                    return new ValorFijoConfiguradoViewModel
                    {
                        IdRelacion = item.Id,
                        IdValorFijo = valorFijo.Id,
                        Descripcion = valorFijo.Descripcion,
                        Tipo = valorFijo.Tipo,
                        Importe = item.Importe
                    };
                })
                .ToList(),
            ValoresCategorias = entity.ValoresCategorias
                .OrderBy(item => catalogs.Categorias[item.CategoriaId].Numero)
                .Select(item =>
                {
                    var categoria = catalogs.Categorias[item.CategoriaId];
                    return new ValorCategoriaConfiguradoViewModel
                    {
                        IdRelacion = item.Id,
                        IdCategoria = categoria.Id,
                        CategoriaDescripcion = categoria.Descripcion,
                        NumeroCategoria = categoria.Numero,
                        Importe = item.Importe
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
