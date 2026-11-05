namespace Nomenclador.Api.DTOs;

public sealed class AsistenteFeedbackResumenDto
{
    public int TotalVotos { get; init; }
    public int Positivos { get; init; }
    public int Negativos { get; init; }
    public IReadOnlyCollection<AsistenteFeedbackMotivoResumenDto> MotivosNegativos { get; init; } = [];
}

public sealed class AsistenteFeedbackMotivoResumenDto
{
    public string Motivo { get; init; } = string.Empty;
    public int Cantidad { get; init; }
}
