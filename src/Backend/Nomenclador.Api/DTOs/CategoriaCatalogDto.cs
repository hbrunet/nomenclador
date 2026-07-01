namespace Nomenclador.Api.DTOs;

public sealed class CategoriaCatalogDto : CatalogItemDto
{
    public int EscalaSalarialId { get; init; }

    public int Numero { get; init; }
}
