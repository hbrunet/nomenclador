using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/grupos-valor-fijo")]
public sealed class GruposValorFijoController(CatalogRepository catalogRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await catalogRepository.GetGruposValorFijoAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await catalogRepository.GetGrupoValorFijoByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GrupoValorFijoCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        if (dto.TiposIds.Count == 0)
            return BadRequest(new { message = "Debe seleccionar al menos un tipo de valor fijo." });

        var result = await catalogRepository.CreateGrupoValorFijoAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoValorFijoCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        var result = await catalogRepository.UpdateGrupoValorFijoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await catalogRepository.DeleteGrupoValorFijoAsync(id);
        return NoContent();
    }
}
