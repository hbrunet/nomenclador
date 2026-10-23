namespace Nomenclador.Api.DTOs;

public sealed class AsistenteMensajeDto
{
    // "user" | "assistant"
    public string Rol { get; init; } = string.Empty;

    public string Contenido { get; init; } = string.Empty;
}
