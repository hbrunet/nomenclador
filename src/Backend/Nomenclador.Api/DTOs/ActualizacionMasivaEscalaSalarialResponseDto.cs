namespace Nomenclador.Api.DTOs;

public sealed class ActualizacionMasivaEscalaSalarialResponseDto
{
    public ActualizacionMasivaEscalaSalarialResultDto? Resultado { get; init; }
    public IReadOnlyCollection<EscalaCloneConflictDto> Conflictos { get; init; } = [];
}
