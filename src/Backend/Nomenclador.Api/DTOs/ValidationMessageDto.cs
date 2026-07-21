namespace Nomenclador.Api.DTOs;

public sealed class ValidationMessageDto
{
    public string Codigo { get; init; } = string.Empty;

    public string Mensaje { get; init; } = string.Empty;

    public string? Campo { get; init; }
}
