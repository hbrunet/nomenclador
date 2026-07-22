using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/valores-categoria")]
public sealed class ValoresCategoriaController(CatalogRepository catalogRepository) : ControllerBase
{
    // ── Tipos ────────────────────────────────────────────────────────────────

    [HttpGet("tipos")]
    public async Task<IActionResult> GetTipos()
        => Ok(await catalogRepository.GetValorCategoriaTiposAsync());

    [HttpPost("tipos")]
    public async Task<IActionResult> CreateTipo([FromBody] ValorCategoriaTipoCreateUpdateDto dto)
        => Ok(await catalogRepository.CreateValorCategoriaTipoAsync(dto));

    [HttpPut("tipos/{id:int}")]
    public async Task<IActionResult> UpdateTipo(int id, [FromBody] ValorCategoriaTipoCreateUpdateDto dto)
    {
        var result = await catalogRepository.UpdateValorCategoriaTipoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("tipos/{id:int}")]
    public async Task<IActionResult> DeleteTipo(int id)
    {
        var deleted = await catalogRepository.DeleteValorCategoriaTipoAsync(id);
        return deleted
            ? NoContent()
            : Conflict(new { message = "El tipo está siendo utilizado por uno o más valores y no puede eliminarse." });
    }

    // ── Valores ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await catalogRepository.GetAllValoresCategoriasListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await catalogRepository.GetValorCategoriaDetailAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ValorCategoriaCreateUpdateDto dto)
    {
        var result = await catalogRepository.CreateValorCategoriaAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ValorCategoriaCreateUpdateDto dto)
    {
        var result = await catalogRepository.UpdateValorCategoriaAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await catalogRepository.DeleteValorCategoriaAsync(id);
        return deleted
            ? NoContent()
            : Conflict(new { message = "El valor está siendo utilizado por una o más configuraciones y no puede eliminarse." });
    }

    // ── Items ────────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/items")]
    public async Task<IActionResult> CreateItem(int id, [FromBody] ValorCategoriaItemCreateUpdateDto dto)
        => Ok(await catalogRepository.CreateValorCategoriaItemAsync(id, dto));

    [HttpPut("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> UpdateItem(int id, int itemId, [FromBody] ValorCategoriaItemCreateUpdateDto dto)
    {
        var result = await catalogRepository.UpdateValorCategoriaItemAsync(itemId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int id, int itemId)
    {
        await catalogRepository.DeleteValorCategoriaItemAsync(itemId);
        return NoContent();
    }
}
