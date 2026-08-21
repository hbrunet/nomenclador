using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Services;

[ApiController]
[Route("api/seg")]
public sealed class SeguridadController(SeguridadService seguridadService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var result = await seguridadService.ValidarUsuarioAsync(request);
        return Ok(result);
    }
}