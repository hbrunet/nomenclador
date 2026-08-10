namespace Nomenclador.Api.DTOs;

public sealed class DesasociacionMasivaResultDto
{
    public int AsociacionesEliminadas { get; init; }
    public int AsociacionesInexistentes { get; init; }
}
