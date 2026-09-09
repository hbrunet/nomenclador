namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresCategoriaResponseDto
{
    public List<ValorCategoriaDetailDto>? Resultado { get; init; }
    public IReadOnlyCollection<ValorCategoriaCloneConflictDto> Conflictos { get; init; } = [];
}
