namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorFijoBusquedaDto
{
    public IReadOnlyCollection<int> TiposIds { get; init; } = [];
    public DateOnly Periodo { get; init; }
}
