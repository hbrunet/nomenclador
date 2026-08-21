using System.Text.Json.Serialization;

namespace Nomenclador.Api.DTOs;

public sealed class ExternalAuthResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
    [JsonPropertyName("data")]
    public AuthData? Data { get; init; }
    [JsonPropertyName("error")]
    public ErrorDetails? Error { get; init; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }
}

public sealed class AuthData
{
    [JsonPropertyName("user")]
    public UserInfo User { get; init; } = new();
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; init; }
}

public sealed class ErrorDetails
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;
    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;
}

public sealed class UserInfo
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("creation_date")]
    public DateTime? CreationDate { get; init; }
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
    [JsonPropertyName("roles")]
    public List<UserRole> Roles { get; init; } = [];
    [JsonPropertyName("is_locked")]
    public bool IsLocked { get; init; }
    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class UserRole
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    [JsonPropertyName("application_id")]
    public int ApplicationId { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("status")]
    public int Status { get; init; }
    [JsonPropertyName("application_name")]
    public string ApplicationName { get; init; } = string.Empty;
}

public sealed class ExternalLoginRequest
{

    [JsonPropertyName("user_name")]
    public string UserName { get; init; } = string.Empty;


    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

 
    [JsonPropertyName("application_id")]
    public int ApplicationId { get; init; }
}
