namespace Nomenclador.Api.DTOs;

public sealed class ValorCategoriaListItemDto : CatalogItemDto
{
    public int IdTipo { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public int CantidadItems { get; init; }
}
