namespace Nomenclador.Api.DTOs;

public sealed class GrupoValorFijoCreateUpdateDto
{
    public string Descripcion { get; init; } = string.Empty;
    public IReadOnlyCollection<int> TiposIds { get; init; } = [];
}
