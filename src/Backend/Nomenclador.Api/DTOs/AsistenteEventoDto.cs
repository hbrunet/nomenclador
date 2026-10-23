namespace Nomenclador.Api.DTOs;

// Evento enviado por SSE al frontend durante una respuesta del asistente.
public sealed class AsistenteEventoDto
{
    // "texto" | "herramienta" | "accion" | "error" | "fin"
    public string Tipo { get; init; } = string.Empty;

    public string? Texto { get; init; }

    public AsistenteAccionPropuestaDto? Accion { get; init; }
}
