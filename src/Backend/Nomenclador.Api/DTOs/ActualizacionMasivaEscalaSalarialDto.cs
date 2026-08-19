namespace Nomenclador.Api.DTOs;

public sealed class ActualizacionMasivaEscalaSalarialDto
{
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }
}
