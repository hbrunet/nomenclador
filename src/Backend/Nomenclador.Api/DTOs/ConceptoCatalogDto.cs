namespace Nomenclador.Api.DTOs;

public sealed class ConceptoCatalogDto
{
    public int Id { get; init; }

    public int Codigo { get; init; } 

    public int Subcodigo { get; init; }

    public string DescripcionBreve { get; init; } = string.Empty;

    public string Descripcion { get; init; } = string.Empty;
}
