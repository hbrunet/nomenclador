using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(CatalogRepository catalogRepository) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get() => Ok("Healthy");

    [AllowAnonymous]
    [HttpGet("testdb")]
    public async Task<IActionResult> TestDb()
    {
        await catalogRepository.GetPeriodoActivoAsync();

        return Ok("Database connection successful");
    }
}