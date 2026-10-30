namespace Nomenclador.Api.DTOs;

public sealed class AsistenteChatRequestDto
{
    public IReadOnlyCollection<AsistenteMensajeDto> Mensajes { get; init; } = [];
}
