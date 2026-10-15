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
    public async Task<IActionResult> GetConceptos([FromQuery] string? q, [FromQuery] int? idTipoConcepto = null)
    {
        return Ok(await conceptoRepository.GetAllAsync(q, idTipoConcepto));
    }

    [HttpGet("paginado")]
    public async Task<IActionResult> GetConceptosPaginado(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] int? idTipoConcepto = null)
    {
        var (items, total) = await conceptoRepository.GetPagedAsync(q, page, pageSize, idTipoConcepto);
        return Ok(new PagedResult<ConceptoCatalogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        });
    }

    [HttpGet("tipos")]
    public async Task<IActionResult> GetTipos()
        => Ok(await conceptoRepository.GetTiposAsync());

    [HttpGet("tipos-liquidacion")]
    public async Task<IActionResult> GetTiposLiquidacion()
        => Ok(await conceptoRepository.GetTiposLiquidacionAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await conceptoRepository.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ConceptoCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        var result = await conceptoRepository.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ConceptoCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        var result = await conceptoRepository.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }
}
