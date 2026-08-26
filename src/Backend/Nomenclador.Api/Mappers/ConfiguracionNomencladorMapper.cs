using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Mappers;

public sealed class ConfiguracionNomencladorMapper
{
    public ConfiguracionNomencladorEntity ToNewEntity(ConfiguracionNomencladorCreateUpdateDto dto)
    {
        return ApplyScalars(new ConfiguracionNomencladorEntity(), dto);
    }

    public ConfiguracionNomencladorEntity Apply(ConfiguracionNomencladorEntity entity, ConfiguracionNomencladorCreateUpdateDto dto)
    {
        return ApplyScalars(entity, dto);
    }

    private static ConfiguracionNomencladorEntity ApplyScalars(ConfiguracionNomencladorEntity entity, ConfiguracionNomencladorCreateUpdateDto dto)
    {
        entity.NomencladorId = dto.IdNomenclador;
        entity.EscalaSalarialId = dto.IdEscalaSalarial;
        entity.ZonaId = dto.IdZona;
        entity.FechaInicio = new DateOnly(dto.FechaInicio.Year, dto.FechaInicio.Month, 1);
        entity.FechaFin = new DateOnly(dto.FechaFin?.Year ?? 9999, dto.FechaFin?.Month ?? 12, 1);

        return entity;
    }

    public void ApplyChildren(ConfiguracionNomencladorEntity entity, ConfiguracionNomencladorCreateUpdateDto dto)
    {
        entity.Conceptos.Clear();
        foreach (var item in dto.Conceptos)
        {
            entity.Conceptos.Add(new ConceptoConfiguradoEntity
            {
                ConfiguracionNomencladorId = entity.Id,
                ConceptoId = item.IdConcepto,
                Orden = item.Orden,
            });
        }

        entity.ValoresFijos.Clear();
        foreach (var item in dto.ValoresFijos)
        {
            entity.ValoresFijos.Add(new ValorFijoConfiguradoEntity
            {
                ConfiguracionNomencladorId = entity.Id,
                ValorFijoId = item.IdValorFijo,
            });
        }

        entity.ValoresCategorias.Clear();
        foreach (var item in dto.ValoresCategorias)
        {
            entity.ValoresCategorias.Add(new ValorCategoriaConfiguradoEntity
            {
                ConfiguracionNomencladorId = entity.Id,
                ValorCategoriaId = item.IdValorCategoria,
            });
        }
    }

    public ConfiguracionNomencladorListItemDto ToListItemDto(ConfiguracionNomencladorEntity entity, CatalogSnapshot catalogs)
    {
        return new ConfiguracionNomencladorListItemDto
        {
            Id = entity.Id,
            NomencladorDescripcion = MapCatalogDescription(catalogs.Nomencladores, entity.NomencladorId, "Nomenclador"),
            EscalaDescripcion = MapCatalogDescription(catalogs.EscalasSalariales, entity.EscalaSalarialId, "Escala"),
            ZonaDescripcion = MapZonaDescription(catalogs.Zonas, entity.ZonaId),
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
            NomencladorDescripcion = MapCatalogDescription(catalogs.Nomencladores, entity.NomencladorId, "Nomenclador"),
            IdEscalaSalarial = entity.EscalaSalarialId,
            EscalaDescripcion = MapCatalogDescription(catalogs.EscalasSalariales, entity.EscalaSalarialId, "Escala"),
            IdZona = entity.ZonaId,
            ZonaDescripcion = MapZonaDescription(catalogs.Zonas, entity.ZonaId),
            FechaInicio = entity.FechaInicio,
            FechaFin = entity.FechaFin,
            Estado = ResolveEstado(entity.FechaInicio, entity.FechaFin),
            Conceptos = entity.Conceptos
                .Select(item =>
                {
                    if (!catalogs.Conceptos.TryGetValue(item.ConceptoId, out var concepto))
                    {
                        return new ConceptoConfiguradoDto
                        {
                            IdConcepto = item.ConceptoId,
                            Codigo = 0,
                            Subcodigo = 0,
                            Descripcion = "Concepto no encontrado en el catálogo",
                            DescripcionBreve = "N/D",
                            Orden = item.Orden,
                        };
                    }

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
                    if (!catalogs.ValoresFijos.TryGetValue(item.ValorFijoId, out var valorFijo))
                    {
                        return new ValorFijoConfiguradoDto
                        {
                            IdValorFijo = item.ValorFijoId,
                            Descripcion = "Valor fijo no encontrado en el catálogo",
                            Tipo = string.Empty,
                            Valor = 0,
                            IdTipo = 0
                        };
                    }

                    return new ValorFijoConfiguradoDto
                    {
                        IdValorFijo = valorFijo.Id,
                        Descripcion = valorFijo.Descripcion,
                        Tipo = valorFijo.Tipo?.Descripcion ?? string.Empty,
                        Valor = valorFijo.Valor,
                        IdTipo = valorFijo.Tipo?.Id ?? 0
                    };
                })
                .ToList(),
            ValoresCategorias = entity.ValoresCategorias
                .Select(item =>
                {
                    var items = item.Items
                        .Select(vc => new ValorCategoriaConfiguradoItemDto
                        {
                            Id = vc.Id,
                            NumeroCategoria = vc.Numero,
                            Importe = vc.Importe,
                        })
                        .ToList();

                    if (!catalogs.ValoresCategorias.TryGetValue(item.ValorCategoriaId, out var valorCategoria))
                    {
                        return new ValorCategoriaConfiguradoDto
                        {
                            IdValorCategoria = item.ValorCategoriaId,
                            Descripcion = "Valor por categoría no encontrado en el catálogo",
                            Tipo = string.Empty,
                            Items = items,
                            IdTipo = 0
                        };
                    }

                    return new ValorCategoriaConfiguradoDto
                    {
                        IdValorCategoria = valorCategoria.Id,
                        Descripcion = valorCategoria.Descripcion,
                        Tipo = valorCategoria.Tipo?.Descripcion ?? string.Empty,
                        Items = items,
                        IdTipo = valorCategoria.Tipo?.Id ?? 0
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

    private static string MapCatalogDescription<TCatalog>(IReadOnlyDictionary<int, TCatalog> catalog, int id, string entityName)
        where TCatalog : CatalogEntityBase
    {
        return catalog.TryGetValue(id, out var item) ? $"{item.Id} - {item.Descripcion}" : $"{entityName} no encontrado en el catálogo";
    }

    private static string MapZonaDescription<TCatalog>(IReadOnlyDictionary<int, TCatalog> catalog, int? id)
        where TCatalog : CatalogEntityBase
    {
        return id.HasValue ? MapCatalogDescription(catalog, id.Value, "Zona") : "Sin zona";
    }

    private static string ResolveEstado(DateOnly fechaInicio, DateOnly? fechaFin)
    {
        var today = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

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
