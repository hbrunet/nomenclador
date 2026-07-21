using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/escalas")]
public sealed class EscalasController(CatalogRepository catalogRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await catalogRepository.GetAllEscalasAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await catalogRepository.GetEscalaDetailAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EscalaCreateUpdateDto dto)
    {
        var result = await catalogRepository.CreateEscalaAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EscalaCreateUpdateDto dto)
    {
        var result = await catalogRepository.UpdateEscalaAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await catalogRepository.DeleteEscalaAsync(id);
        return deleted
            ? NoContent()
            : Conflict(new { message = "La escala está siendo utilizada por una o más configuraciones y no puede eliminarse." });
    }

    [HttpPost("{id:int}/categorias")]
    public async Task<IActionResult> CreateCategoria(int id, [FromBody] CategoriaCreateUpdateDto dto)
        => Ok(await catalogRepository.CreateCategoriaAsync(id, dto));

    [HttpPut("{id:int}/categorias/{catId:int}")]
    public async Task<IActionResult> UpdateCategoria(int id, int catId, [FromBody] CategoriaCreateUpdateDto dto)
    {
        var result = await catalogRepository.UpdateCategoriaAsync(catId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}/categorias/{catId:int}")]
    public async Task<IActionResult> DeleteCategoria(int id, int catId)
    {
        await catalogRepository.DeleteCategoriaAsync(catId);
        return NoContent();
    }
}
