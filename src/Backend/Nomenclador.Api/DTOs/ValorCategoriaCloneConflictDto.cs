namespace Nomenclador.Api.DTOs;

// Se devuelve cuando ya existe, para el mismo Tipo, un valor por categoría con el nombre que
// tendría el clon (mismo período, vía ReemplazarPeriodoEnDescripcion) — evita un duplicado.
public sealed class ValorCategoriaCloneConflictDto
{
    public int OriginalId { get; init; }
    public string OriginalDescripcion { get; init; } = string.Empty;
    public int ExistenteId { get; init; }
    public string ExistenteDescripcion { get; init; } = string.Empty;
}
