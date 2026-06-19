using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/conceptos")]
public sealed class ConceptosController(ConceptoRepository conceptoRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConceptos([FromQuery] string? q)
    {
        return Ok(await conceptoRepository.GetAllAsync(q));
    }
}
