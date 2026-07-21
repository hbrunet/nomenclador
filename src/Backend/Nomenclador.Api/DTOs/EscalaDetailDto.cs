namespace Nomenclador.Api.DTOs;

public sealed class EscalaDetailDto : CatalogItemDto
{
    public IReadOnlyCollection<CategoriaCatalogDto> Categorias { get; init; } = [];
}
