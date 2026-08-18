using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/grupos-valor-categoria")]
public sealed class GruposValorCategoriaController(CatalogRepository catalogRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await catalogRepository.GetGruposValorCategoriaAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await catalogRepository.GetGrupoValorCategoriaByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GrupoValorCategoriaCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        if (dto.TiposIds.Count == 0)
            return BadRequest(new { message = "Debe seleccionar al menos un tipo de valor por categoría." });

        var result = await catalogRepository.CreateGrupoValorCategoriaAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoValorCategoriaCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        var result = await catalogRepository.UpdateGrupoValorCategoriaAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await catalogRepository.DeleteGrupoValorCategoriaAsync(id);
        return NoContent();
    }
}
