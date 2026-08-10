namespace Nomenclador.Api.DTOs;

public sealed class AsociacionMasivaValoresFijosDto
{
    public IReadOnlyCollection<int> ValoresFijosIds { get; init; } = [];
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
}
