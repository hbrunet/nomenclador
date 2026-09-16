using System.Net;
using System.Text.Json;
using Nomenclador.Api.Services;
using Serilog.Context;

namespace Nomenclador.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var userName = GetUserName(context);
        context.Response.Headers["X-Request-Id"] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? string.Empty))
        using (LogContext.PushProperty("UserName", userName))
        {
            try
            {
                await next(context);
                LogInformation(context, context.Response.StatusCode, "Solicitud finalizada correctamente.");
            }
            catch (ConfiguracionValidationException exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                LogWarning(context, exception, (int)HttpStatusCode.BadRequest, "Validación de configuración fallida.");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(exception.Response, SerializerOptions));
            }
            catch (KeyNotFoundException exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                LogWarning(context, exception, (int)HttpStatusCode.NotFound, "Recurso no encontrado.");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    mensaje = exception.Message
                }, SerializerOptions));
            }
            catch (UnauthorizedException exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                LogWarning(context, exception, (int)HttpStatusCode.Unauthorized, "Acceso no autorizado.");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    mensaje = exception.Message
                }, SerializerOptions));
            }
            catch (ForbiddenException exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                LogWarning(context, exception, (int)HttpStatusCode.Forbidden, "Acceso prohibido.");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    mensaje = exception.Message
                }, SerializerOptions));
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                LogError(context, exception, (int)HttpStatusCode.InternalServerError, "Error inesperado en la API.");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    mensaje = "Ocurrió un error inesperado al procesar la solicitud.",
                    detalle = BuildDetailMessage(exception)
                }, SerializerOptions));
            }
        }
    }

    private static string GetUserName(HttpContext context)
    {
        return context.User?.Identity?.Name
            ?? context.User?.FindFirst("name")?.Value
            ?? context.User?.FindFirst("preferred_username")?.Value
            ?? "anonymous";
    }

    private void LogInformation(HttpContext context, int statusCode, string message)
    {
        logger.LogInformation(
            "{Message} Method={Method} Path={Path} StatusCode={StatusCode} RequestId={RequestId} User={UserName}",
            message,
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            statusCode,
            context.TraceIdentifier,
            GetUserName(context));
    }

    private void LogWarning(HttpContext context, Exception exception, int statusCode, string message)
    {
        logger.LogWarning(
            exception,
            "{Message} Method={Method} Path={Path} StatusCode={StatusCode} RequestId={RequestId} User={UserName}",
            message,
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            statusCode,
            context.TraceIdentifier,
            GetUserName(context));
    }

    private void LogError(HttpContext context, Exception exception, int statusCode, string message)
    {
        logger.LogError(
            exception,
            "{Message} Method={Method} Path={Path} StatusCode={StatusCode} RequestId={RequestId} User={UserName}",
            message,
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            statusCode,
            context.TraceIdentifier,
            GetUserName(context));
    }

    private static string BuildDetailMessage(Exception ex)
    {
        var parts = new System.Text.StringBuilder();
        var current = ex;
        while (current is not null)
        {
            if (parts.Length > 0) parts.Append(" --> ");
            parts.Append(current.Message);
            current = current.InnerException;
        }
        return parts.ToString();
    }
}
