namespace Nomenclador.Api.Models;

public sealed class CatalogSnapshot
{
    public required IReadOnlyDictionary<int, NomencladorCatalogEntity> Nomencladores { get; init; }

    public required IReadOnlyDictionary<int, EscalaSalarialCatalogEntity> EscalasSalariales { get; init; }

    public required IReadOnlyDictionary<int, ZonaCatalogEntity> Zonas { get; init; }

    public required IReadOnlyDictionary<int, CategoriaCatalogEntity> Categorias { get; init; }

    public required IReadOnlyDictionary<int, ConceptoCatalogEntity> Conceptos { get; init; }

    public required IReadOnlyDictionary<int, ValorFijoCatalogEntity> ValoresFijos { get; init; }
}
