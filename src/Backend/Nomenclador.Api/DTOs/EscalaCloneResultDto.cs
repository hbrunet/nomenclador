namespace Nomenclador.Api.DTOs;

public sealed class EscalaCloneResultDto
{
    public EscalaDetailDto? Escala { get; init; }
    public EscalaCloneConflictDto? Conflicto { get; init; }
}
