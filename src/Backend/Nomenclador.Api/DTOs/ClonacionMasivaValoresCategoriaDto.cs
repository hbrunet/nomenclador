namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresCategoriaDto
{
    public IReadOnlyCollection<int> ValoresCategoriaIds { get; init; } = [];
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }
    public bool ActualizarValoresExistentes { get; init; }
}
