using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Services;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/configuraciones-nomenclador")]
public sealed class ConfiguracionesNomencladorController(ConfiguracionNomencladorService configuracionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetConfiguraciones(
        [FromQuery] int? nomencladorId,
        [FromQuery] int? escalaSalarialId,
        [FromQuery] int? zonaId,
        [FromQuery] DateOnly? vigenteEn,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await configuracionService.GetAllAsync(nomencladorId, escalaSalarialId, zonaId, vigenteEn, estado, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetConfiguracion(int id)
    {
        return Ok(await configuracionService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> CreateConfiguracion([FromBody] ConfiguracionNomencladorCreateUpdateDto request)
    {
        var created = await configuracionService.CreateAsync(request);
        return CreatedAtAction(nameof(GetConfiguracion), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateConfiguracion(int id, [FromBody] ConfiguracionNomencladorCreateUpdateDto request)
    {
        return Ok(await configuracionService.UpdateAsync(id, request));
    }

    [HttpPost("validar")]
    public async Task<IActionResult> ValidarConfiguracion([FromBody] ConfiguracionNomencladorCreateUpdateDto request)
    {
        return Ok(await configuracionService.ValidateAsync(request));
    }

    [HttpPost("{id:int}/clonar")]
    public async Task<IActionResult> ClonarConfiguracion(int id, [FromBody] ClonarConfiguracionDto request)
    {
        var created = await configuracionService.CloneAsync(id, request);
        return CreatedAtAction(nameof(GetConfiguracion), new { id = created.Id }, created);
    }
}
