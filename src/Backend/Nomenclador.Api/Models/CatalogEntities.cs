namespace Nomenclador.Api.Models;

public abstract class CatalogEntityBase
{
    public int Id { get; init; }

    public string Descripcion { get; init; } = string.Empty;
}

public sealed class NomencladorCatalogEntity : CatalogEntityBase;

public sealed class EscalaSalarialCatalogEntity : CatalogEntityBase;

public sealed class ZonaCatalogEntity : CatalogEntityBase;

public sealed class CategoriaCatalogEntity : CatalogEntityBase
{
    public int EscalaSalarialId { get; init; }

    public int Numero { get; init; }
}

public sealed class ConceptoCatalogEntity
{
    public int Id { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public int Subcodigo { get; init; }

    public string DescripcionBreve { get; init; } = string.Empty;

    public string Descripcion { get; init; } = string.Empty;

    public string Clasificacion { get; init; } = string.Empty;
}

public sealed class ValorFijoCatalogEntity : CatalogEntityBase
{
    public string Tipo { get; init; } = string.Empty;
}
