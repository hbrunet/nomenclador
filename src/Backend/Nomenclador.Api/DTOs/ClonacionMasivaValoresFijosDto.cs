namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresFijosDto
{
    public int[] ValoresFijosIds { get; set; } = Array.Empty<int>();
    public DateOnly NuevoPeriodo { get; set; }
    public decimal CoeficienteAjuste { get; set; }
}