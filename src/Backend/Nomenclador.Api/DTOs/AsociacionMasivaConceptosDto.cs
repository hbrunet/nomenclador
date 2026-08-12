namespace Nomenclador.Api.DTOs;

public sealed class AsociacionMasivaConceptosDto
{
    public IReadOnlyCollection<int> ConceptosIds { get; init; } = [];
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
}