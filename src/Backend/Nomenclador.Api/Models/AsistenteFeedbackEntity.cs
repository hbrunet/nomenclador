namespace Nomenclador.Api.Models;

// Valoración (👍/👎) de una respuesta puntual del asistente. RespuestaId se genera en el
// frontend (no hay historial de conversación persistido) y permite cambiar el voto de una
// misma respuesta. Herramientas y, si el usuario lo autoriza, la conversación completa quedan
// guardadas para poder ajustar el prompt o el manual de ayuda a partir de casos concretos.
public class AsistenteFeedbackEntity
{
    public virtual int Id { get; set; }

    public virtual string RespuestaId { get; set; } = string.Empty;

    public virtual string Usuario { get; set; } = string.Empty;

    public virtual bool Util { get; set; }

    public virtual string? Motivo { get; set; }

    public virtual string? Comentario { get; set; }

    public virtual string? Herramientas { get; set; }

    public virtual string? ConsultaUsuario { get; set; }

    public virtual string? RespuestaAsistente { get; set; }

    public virtual string Modelo { get; set; } = string.Empty;

    public virtual DateTime Fecha { get; set; }
}
