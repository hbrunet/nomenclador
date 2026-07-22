namespace Nomenclador.Api.DTOs;

public sealed class ValorCategoriaDetailDto : CatalogItemDto
{
    public int IdTipo { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public IReadOnlyCollection<ValorCategoriaConfiguradoItemDto> Items { get; init; } = [];
}
