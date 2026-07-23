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

    public ConfiguracionNomencladorListItemDto ToListItemDto(ConfiguracionNomencladorEntity entity, CatalogSnapshot catalogs)
    {
        return new ConfiguracionNomencladorListItemDto
        {
            Id = entity.Id,
            NomencladorDescripcion = MapCatalogDescription(catalogs.Nomencladores, entity.NomencladorId),
            EscalaDescripcion = MapCatalogDescription(catalogs.EscalasSalariales, entity.EscalaSalarialId),
            ZonaDescripcion = MapCatalogDescription(catalogs.Zonas, entity.ZonaId),
            FechaInicio = entity.FechaInicio,
            FechaFin = entity.FechaFin,
            Estado = ResolveEstado(entity.FechaInicio, entity.FechaFin),
        };
    }

    public ConfiguracionNomencladorDetailDto ToDetailDto(ConfiguracionNomencladorEntity entity, CatalogSnapshot catalogs)
    {
        return new ConfiguracionNomencladorDetailDto
        {
            Id = entity.Id,
            IdNomenclador = entity.NomencladorId,
            NomencladorDescripcion = MapCatalogDescription(catalogs.Nomencladores, entity.NomencladorId),
            IdEscalaSalarial = entity.EscalaSalarialId,
            EscalaDescripcion = MapCatalogDescription(catalogs.EscalasSalariales, entity.EscalaSalarialId),
            IdZona = entity.ZonaId,
            ZonaDescripcion = MapCatalogDescription(catalogs.Zonas, entity.ZonaId),
            FechaInicio = entity.FechaInicio,
            FechaFin = entity.FechaFin,
            Estado = ResolveEstado(entity.FechaInicio, entity.FechaFin),
            Conceptos = entity.Conceptos
                .Select(item =>
                {
                    var concepto = catalogs.Conceptos[item.ConceptoId];
                    return new ConceptoConfiguradoDto
                    {
                        IdConcepto = concepto.Id,
                        Codigo = concepto.Codigo,
                        Subcodigo = concepto.Subcodigo,
                        Descripcion = concepto.Descripcion,
                        Orden = item.Orden,
                        DescripcionBreve = concepto.DescripcionBreve,
                    };
                })
                .OrderBy(item => item.Codigo)
                .ThenBy(item => item.Subcodigo)
                .ToList(),
            ValoresFijos = entity.ValoresFijos
                .Select(item =>
                {
                    var valorFijo = catalogs.ValoresFijos[item.ValorFijoId];
                    return new ValorFijoConfiguradoDto
                    {
                        IdValorFijo = valorFijo.Id,
                        Descripcion = valorFijo.Descripcion,
                        Tipo = valorFijo.Tipo?.Descripcion ?? string.Empty,
                        Valor = valorFijo.Valor,
                    };
                })
                .ToList(),
            ValoresCategorias = entity.ValoresCategorias
                .Select(item =>
                {
                    var valorCategoria = catalogs.ValoresCategorias[item.ValorCategoriaId];
                    return new ValorCategoriaConfiguradoDto
                    {
                        IdValorCategoria = valorCategoria.Id,
                        Descripcion = valorCategoria.Descripcion,
                        Tipo = valorCategoria.Tipo?.Descripcion ?? string.Empty,
                        Items = item.Items
                            .Select(vc => new ValorCategoriaConfiguradoItemDto
                            {
                                Id = vc.Id,
                                NumeroCategoria = vc.Numero,
                                Importe = vc.Importe,
                            })
                            .ToList()
                    };
                })
                .ToList(),
            Categorias = catalogs.Categorias.Values
                .Where(item => item.EscalaSalarialId == entity.EscalaSalarialId)
                .Select(item => new CategoriaCatalogDto
                {
                    Id = item.Id,
                    EscalaSalarialId = item.EscalaSalarialId,
                    Numero = item.Numero,
                    Monto = item.Monto,
                    Descripcion = item.Descripcion
                })
                .ToList()
        };
    }

    private static string MapCatalogDescription<TCatalog>(IReadOnlyDictionary<int, TCatalog> catalog, int id)
        where TCatalog : CatalogEntityBase
    {
        return catalog.TryGetValue(id, out var item) ? item.Descripcion : "Sin catálogo";
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
