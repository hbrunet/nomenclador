namespace Nomenclador.Api.DTOs;

// Se devuelve cuando ya existe una escala con el nombre que tendría el clon (mismo período,
// vía ReemplazarPeriodoEnDescripcion) para evitar crear un duplicado accidental.
public sealed class EscalaCloneConflictDto
{
    public int EscalaOriginalId { get; init; }
    public string EscalaOriginalDescripcion { get; init; } = string.Empty;
    public int EscalaExistenteId { get; init; }
    public string EscalaExistenteDescripcion { get; init; } = string.Empty;

    // Solo poblado en la actualización masiva (indica qué configuraciones seleccionadas
    // referencian la escala original en conflicto).
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
}
