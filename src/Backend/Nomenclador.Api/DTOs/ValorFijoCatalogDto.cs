namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoCatalogDto : CatalogItemDto
{
    public int IdTipo { get; init; }

    public string Tipo { get; init; } = string.Empty;

    public decimal Valor { get; init; }
}
