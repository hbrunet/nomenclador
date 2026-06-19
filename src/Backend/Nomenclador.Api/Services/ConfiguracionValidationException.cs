using Nomenclador.Api.DTOs;

namespace Nomenclador.Api.Services;

public sealed class ConfiguracionValidationException(ValidacionConfiguracionResponse response) : Exception("La configuración no superó las validaciones.")
{
    public ValidacionConfiguracionResponse Response { get; } = response;
}
