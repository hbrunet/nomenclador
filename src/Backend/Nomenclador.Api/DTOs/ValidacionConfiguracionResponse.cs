namespace Nomenclador.Api.DTOs;

public sealed class ValidacionConfiguracionResponse
{
    public bool Valida { get; init; }

    public IReadOnlyCollection<ValidationMessageDto> Errores { get; init; } = [];

    public IReadOnlyCollection<ValidationMessageDto> Warnings { get; init; } = [];
}
