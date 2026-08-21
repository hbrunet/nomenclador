namespace Nomenclador.Api.DTOs;

public sealed class LoginResultDto
{
    public string TokenType { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}
