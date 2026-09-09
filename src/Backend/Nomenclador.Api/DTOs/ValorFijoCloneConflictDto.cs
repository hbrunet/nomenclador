namespace Nomenclador.Api.DTOs;

// Se devuelve cuando ya existe, para el mismo Tipo, un valor fijo con el nombre que tendría
// el clon (mismo período, vía ReemplazarPeriodoEnDescripcion) — evita crear un duplicado.
public sealed class ValorFijoCloneConflictDto
{
    public int OriginalId { get; init; }
    public string OriginalDescripcion { get; init; } = string.Empty;
    public int ExistenteId { get; init; }
    public string ExistenteDescripcion { get; init; } = string.Empty;
}
