namespace Nomenclador.Api.Services;

public sealed class UnauthorizedException(string message) : Exception(message);
