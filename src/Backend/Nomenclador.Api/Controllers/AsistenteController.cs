using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Services;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/asistente")]
public sealed class AsistenteController(AsistenteService asistenteService) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("estado")]
    public IActionResult GetEstado() => Ok(new { habilitado = asistenteService.Habilitado });

    // Respuesta en streaming con Server-Sent Events: cada evento es un AsistenteEventoDto serializado en JSON.
    [HttpPost("chat")]
    public async Task Chat([FromBody] AsistenteChatRequestDto request, CancellationToken cancellationToken)
    {
        if (!asistenteService.Habilitado)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(
                new { mensaje = "El asistente no está configurado en el servidor." }, cancellationToken);
            return;
        }

        if (request.Mensajes.Count == 0 || request.Mensajes.Last().Rol != "user")
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { mensaje = "La conversación debe terminar con un mensaje del usuario." }, cancellationToken);
            return;
        }

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var evento in asistenteService.ChatAsync(request.Mensajes, cancellationToken))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(evento, SerializerOptions)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
