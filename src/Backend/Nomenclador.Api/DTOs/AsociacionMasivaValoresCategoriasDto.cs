namespace Nomenclador.Api.DTOs;

public sealed class AsociacionMasivaValoresCategoriasDto
{
    public IReadOnlyCollection<int> ValoresCategoriasIds { get; init; } = [];
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
}
