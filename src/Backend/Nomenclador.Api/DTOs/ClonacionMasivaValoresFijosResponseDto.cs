namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresFijosResponseDto
{
    public List<ValorFijoCatalogDto>? Resultado { get; init; }
    public IReadOnlyCollection<ValorFijoCloneConflictDto> Conflictos { get; init; } = [];
}
