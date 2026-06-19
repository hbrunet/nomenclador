using System.Net;
using System.Text.Json;
using Nomenclador.Api.Services;

namespace Nomenclador.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ConfiguracionValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(exception.Response, SerializerOptions));
        }
        catch (KeyNotFoundException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                mensaje = exception.Message
            }, SerializerOptions));
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                mensaje = "Ocurrió un error inesperado al procesar la solicitud.",
                detalle = exception.Message
            }, SerializerOptions));
        }
    }
}
