using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;


namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/valores-fijos")]
public sealed class ValoresFijosController(CatalogRepository catalogRepository) : ControllerBase
{
    // ── Tipos ────────────────────────────────────────────────────────────────

    [HttpGet("tipos")]
    public async Task<IActionResult> GetTipos()
        => Ok(await catalogRepository.GetValorFijoTiposAsync());

    [HttpPost("tipos")]
    public async Task<IActionResult> CreateTipo([FromBody] CatalogItemDto dto)
        => Ok(await catalogRepository.CreateValorFijoTipoAsync(dto));

    [HttpPut("tipos/{id:int}")]
    public async Task<IActionResult> UpdateTipo(int id, [FromBody] CatalogItemDto dto)
    {
        var result = await catalogRepository.UpdateValorFijoTipoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("tipos/{id:int}")]
    public async Task<IActionResult> DeleteTipo(int id)
    {
        var deleted = await catalogRepository.DeleteValorFijoTipoAsync(id);
        return deleted
            ? NoContent()
            : Conflict(new { message = "El tipo está siendo utilizado por uno o más valores y no puede eliminarse." });
    }

    // ── Valores ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await catalogRepository.GetAllValoresFijosListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await catalogRepository.GetValorFijoDetailAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ValorFijoCreateDto dto)
    {
        var result = await catalogRepository.CreateValorFijoAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ValorFijoCreateDto dto)
    {
        var result = await catalogRepository.UpdateValorFijoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await catalogRepository.DeleteValorFijoAsync(id);
        return deleted
            ? NoContent()
            : Conflict(new { message = "El valor está siendo utilizado por una o más configuraciones y no puede eliminarse." });
    }

    [HttpPost("{id:int}/clonar")]
    public async Task<IActionResult> Clone(int id, [FromBody] ValorFijoCloneDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest(new { message = "La descripción es obligatoria." });

        if (dto.CoeficienteAjuste <= 0)
            return BadRequest(new { message = "El coeficiente de ajuste debe ser mayor a cero." });

        var result = await catalogRepository.CloneValorFijoAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("clonacion-masiva")]
    public async Task<IActionResult> CloneMasivo([FromBody] ClonacionMasivaValoresFijosDto dto)
    {
        if (!dto.ActualizarValoresExistentes && dto.NuevoPeriodo == default)
            return BadRequest(new { message = "El nuevo período es obligatorio." });

        if (dto.CoeficienteAjuste <= 0)
            return BadRequest(new { message = "El coeficiente de ajuste debe ser mayor a cero." });
        
        if (dto.ValoresFijosIds.Count == 0)
            return BadRequest(new { message = "Debe seleccionar al menos un valor fijo para clonar." });

        var result = await catalogRepository.CloneValoresFijosMasivoAsync(dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("buscar-por-tipo-y-periodo")]
    public async Task<IActionResult> BuscarPorTipoYPeriodo([FromBody] SustitucionValorFijoBusquedaDto dto)
    {
        if (dto.TiposIds.Count == 0)
            return BadRequest(new { message = "Debe indicar al menos un tipo para buscar." });

        if (dto.Periodo == default)
            return BadRequest(new { message = "El período es obligatorio." });

        return Ok(await catalogRepository.BuscarValoresFijosPorTipoYPeriodoAsync(dto.TiposIds, dto.Periodo));
    }
}

