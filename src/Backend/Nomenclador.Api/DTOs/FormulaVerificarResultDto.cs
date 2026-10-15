namespace Nomenclador.Api.DTOs;

public sealed class FormulaVerificarResultDto
{
    public bool Valida { get; init; }

    public IReadOnlyList<string> Errores { get; init; } = [];
}
