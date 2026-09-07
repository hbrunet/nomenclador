namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorCategoriaBusquedaDto
{
    public IReadOnlyCollection<int> TiposIds { get; init; } = [];
    public DateOnly Periodo { get; init; }
}
