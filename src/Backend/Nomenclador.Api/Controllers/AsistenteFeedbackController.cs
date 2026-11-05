using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;
using Nomenclador.Api.Repositories;
using Nomenclador.Api.Services;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/asistente/feedback")]
public sealed class AsistenteFeedbackController(
    AsistenteFeedbackRepository repository,
    IOptions<AsistenteOptions> asistenteOptions) : ControllerBase
{
    // Mismos límites de texto que el resto del asistente (ver AsistenteService.MaxLargoMensaje
    // para ConsultaUsuario); RespuestaAsistente puede ser más larga por el Markdown generado.
    private const int MaxLargoComentario = 2000;
    private const int MaxLargoConsulta = 4000;
    private const int MaxLargoRespuesta = 8000;
    private static readonly HashSet<string> MotivosValidos =
        new(StringComparer.OrdinalIgnoreCase) { "dato_incorrecto", "respuesta_incompleta", "no_entendio", "otro" };

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AsistenteFeedbackCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RespuestaId))
            return BadRequest(new { mensaje = "Falta el identificador de la respuesta." });

        if (!dto.Util && (string.IsNullOrWhiteSpace(dto.Motivo) || !MotivosValidos.Contains(dto.Motivo)))
            return BadRequest(new { mensaje = "Para un voto negativo, indicá un motivo válido." });

        var entity = new AsistenteFeedbackEntity
        {
            RespuestaId = dto.RespuestaId,
            Usuario = User.Identity?.Name ?? "desconocido",
            Util = dto.Util,
            Motivo = dto.Util ? null : dto.Motivo!.ToLowerInvariant(),
            Comentario = Truncar(dto.Comentario, MaxLargoComentario),
            Herramientas = Truncar(string.Join(", ", dto.Herramientas), 500),
            ConsultaUsuario = Truncar(dto.ConsultaUsuario, MaxLargoConsulta),
            RespuestaAsistente = Truncar(dto.RespuestaAsistente, MaxLargoRespuesta),
            Modelo = asistenteOptions.Value.Model,
            Fecha = DateTime.Now,
        };

        await repository.CreateOrUpdateAsync(entity);
        return NoContent();
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen() => Ok(await repository.GetResumenAsync());

    private static string? Truncar(string? texto, int maxLargo) =>
        string.IsNullOrEmpty(texto) ? null : texto.Length > maxLargo ? texto[..maxLargo] : texto;
}
