using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;
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

    [HttpGet("paginado")]
    public async Task<IActionResult> GetConceptosPaginado(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var (items, total) = await conceptoRepository.GetPagedAsync(q, page, pageSize);
        return Ok(new PagedResult<ConceptoCatalogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        });
    }
}
