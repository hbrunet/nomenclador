namespace Nomenclador.Api.DTOs;

public sealed class GrupoValorFijoDto
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public IReadOnlyCollection<CatalogItemDto> Tipos { get; init; } = [];
}
