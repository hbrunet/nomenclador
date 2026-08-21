namespace Nomenclador.Api.Services;

public sealed class ForbiddenException(string message) : Exception(message);
