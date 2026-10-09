namespace Nomenclador.Api.DTOs;

public sealed class FormulaDto
{
    public int Id { get; init; }

    public string? Condicion { get; init; }

    public string? Accion { get; init; }

    public int? OrdenEjec { get; init; }

    public decimal? CondicionInput { get; init; }

    public decimal? AccionInput { get; init; }

    public string? Codigo { get; init; }

    public string? SpName { get; init; }
}
