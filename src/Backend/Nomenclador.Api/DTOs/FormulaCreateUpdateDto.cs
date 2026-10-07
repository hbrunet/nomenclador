namespace Nomenclador.Api.DTOs;

public sealed class FormulaCreateUpdateDto
{
    // Solo se usa en Create; en Update el concepto de una fórmula no se reasigna.
    public int ConceptoId { get; init; }

    public string Condicion { get; init; } = string.Empty;

    public string Accion { get; init; } = string.Empty;
}
