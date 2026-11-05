namespace Nomenclador.Api.DTOs;

public sealed class AsistenteFeedbackCreateDto
{
    public string RespuestaId { get; init; } = string.Empty;
    public bool Util { get; init; }
    public string? Motivo { get; init; }
    public string? Comentario { get; init; }
    public IReadOnlyCollection<string> Herramientas { get; init; } = [];
    public string? ConsultaUsuario { get; init; }
    public string? RespuestaAsistente { get; init; }
}
