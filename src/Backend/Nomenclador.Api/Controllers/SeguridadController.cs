using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Services;

[ApiController]
[Route("api/seg")]
public sealed class SeguridadController(SeguridadService seguridadService) : ControllerBase
{
    private const string AuthCookieName = "nomenclador.auth";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var (token, response) = await seguridadService.ValidarUsuarioAsync(request);

        Response.Cookies.Append(AuthCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = response.ExpiresAt,
            IsEssential = true,
            Path = "/",
        });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            IsEssential = true,
            Path = "/",
        });

        return NoContent();
    }
}