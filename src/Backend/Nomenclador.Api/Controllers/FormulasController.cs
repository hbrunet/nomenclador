using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/formulas")]
public sealed class FormulasController(FormulaRepository formulaRepository) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await formulaRepository.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FormulaCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Condicion) || string.IsNullOrWhiteSpace(dto.Accion))
            return BadRequest(new { message = "La condición y la acción son obligatorias." });

        var erroresSintaxis = FormulaRepository.ValidarSintaxis(dto.Condicion, dto.Accion);
        if (erroresSintaxis.Count > 0)
            return BadRequest(new { message = string.Join(" ", erroresSintaxis) });

        var result = await formulaRepository.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] FormulaCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Condicion) || string.IsNullOrWhiteSpace(dto.Accion))
            return BadRequest(new { message = "La condición y la acción son obligatorias." });

        var erroresSintaxis = FormulaRepository.ValidarSintaxis(dto.Condicion, dto.Accion);
        if (erroresSintaxis.Count > 0)
            return BadRequest(new { message = string.Join(" ", erroresSintaxis) });

        var result = await formulaRepository.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await formulaRepository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar([FromBody] FormulaVerificarDto dto)
    {
        return Ok(await formulaRepository.VerificarAsync(dto));
    }
}
