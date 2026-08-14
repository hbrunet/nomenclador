namespace Nomenclador.Api.DTOs;

public sealed class ValorCategoriaCatalogDto : CatalogItemDto
{
    public int IdTipo { get; init; }

    public string Tipo { get; init; } = string.Empty;
}
