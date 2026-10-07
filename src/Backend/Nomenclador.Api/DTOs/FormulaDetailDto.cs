namespace Nomenclador.Api.DTOs;

public sealed class FormulaDetailDto
{
    public int Id { get; init; }

    public int ConceptoId { get; init; }

    public int ConceptoCodigo { get; init; }

    public int ConceptoSubcodigo { get; init; }

    public string ConceptoDescripcion { get; init; } = string.Empty;

    public string Condicion { get; init; } = string.Empty;

    public string Accion { get; init; } = string.Empty;

    public int? OrdenEjec { get; init; }

    public string? SpName { get; init; }
}
