using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Nomenclador.Api.DTOs;

namespace Nomenclador.Api.Services;

public sealed class SeguridadService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<LoginResultDto> ValidarUsuarioAsync(LoginDto credenciales)
    {
        var loginUrl = configuration["ExternalAuth:LoginUrl"]
            ?? throw new InvalidOperationException("La configuración 'ExternalAuth:LoginUrl' no está definida.");
        var applicationId = configuration.GetValue<int?>("ExternalAuth:ApplicationId")
            ?? throw new InvalidOperationException("La configuración 'ExternalAuth:ApplicationId' no está definida.");

        var externalRequest = new ExternalLoginRequest
        {
            UserName = $"h_{credenciales.Username}",
            Password = credenciales.Password,
            ApplicationId = applicationId,
        };

        var response = await httpClient.PostAsJsonAsync(loginUrl, externalRequest);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Usuario o contraseña incorrectos.");
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException("El usuario no tiene permisos para acceder a la aplicación.");
            }
            else
            {
                throw new InvalidOperationException($"Error al llamar al servicio externo de autenticación. Código de estado: {response.StatusCode}");
            }
        }

        var authResponse = await response.Content.ReadFromJsonAsync<ExternalAuthResponse>();
        if (authResponse is null)
        {
            throw new InvalidOperationException("La respuesta del servicio externo de autenticación es nula o no se pudo deserializar.");
        }

        if (!authResponse.Success)
        {
            throw new InvalidOperationException($"Autenticación fallida: {authResponse.Message}");
        }

        if (authResponse.Data?.User.IsLocked is true)
        {
            throw new InvalidOperationException("El usuario está bloqueado y no puede iniciar sesión.");
        }

        if (authResponse.Data?.User.Roles is null || !authResponse.Data.User.Roles.Any(r => r.ApplicationId == applicationId))
        {
            throw new InvalidOperationException("El usuario no tiene roles asignados para la aplicación y no puede iniciar sesión.");
        }

        // El servicio externo solo se usa para validar credenciales; la sesión contra esta API
        // se maneja con un JWT propio, firmado con una clave que controlamos íntegramente.
        var (token, expiresAt) = GenerarToken(credenciales.Username, authResponse.Data.User);

        return new LoginResultDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            DisplayName = authResponse.Data.User.DisplayName,
        };
    }

    private (string Token, DateTime ExpiresAt) GenerarToken(string username, UserInfo user)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("La configuración 'Jwt:SigningKey' no está definida.");
        var expirationHours = configuration.GetValue<double?>("Jwt:ExpirationHours") ?? 8;
        var expiresAt = DateTime.UtcNow.AddHours(expirationHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("display_name", user.DisplayName),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}