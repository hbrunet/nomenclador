namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresFijosDto
{
    public IReadOnlyCollection<int> ValoresFijosIds { get; init; } = [];
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }
    public bool ActualizarValoresExistentes { get; init; }
}