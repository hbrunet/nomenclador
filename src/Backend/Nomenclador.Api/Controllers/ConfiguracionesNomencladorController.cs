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
    public async Task<IActionResult> ValidarConfiguracion([FromBody] ConfiguracionNomencladorCreateUpdateDto request, [FromQuery] int? excludedId = null)
    {
        return Ok(await configuracionService.ValidateAsync(request, excludedId));
    }

    [HttpPost("{id:int}/concepto")]
    public async Task<IActionResult> AgregarConcepto(int id, [FromBody] ConceptoConfiguradoInputDto request)
    {
        return Ok(await configuracionService.AddConceptoAsync(id, request));
    }

    [HttpDelete("{id:int}/concepto/{conceptoId:int}")]
    public async Task<IActionResult> EliminarConcepto(int id, int conceptoId)
    {
        return Ok(await configuracionService.RemoveConceptoAsync(id, conceptoId));
    }


    [HttpPost("{id:int}/valor-fijo")]
    public async Task<IActionResult> AgregarValorFijo(int id, [FromBody] ValorFijoConfiguradoInputDto request)
    {
        return Ok(await configuracionService.AddValorFijoAsync(id, request));
    }

    [HttpDelete("{id:int}/valor-fijo/{valorFijoId:int}")]
    public async Task<IActionResult> EliminarValorFijo(int id, int valorFijoId)
    {
        return Ok(await configuracionService.RemoveValorFijoAsync(id, valorFijoId));
    }

    [HttpPost("asociacion-masiva-valores-fijos")]
    public async Task<IActionResult> AsociarValoresFijosMasivo([FromBody] AsociacionMasivaValoresFijosDto request)
    {
        return Ok(await configuracionService.AsociarValoresFijosMasivoAsync(request));
    }

    [HttpPost("desasociacion-masiva-valores-fijos")]
    public async Task<IActionResult> DesasociarValoresFijosMasivo([FromBody] AsociacionMasivaValoresFijosDto request)
    {
        return Ok(await configuracionService.DesasociarValoresFijosMasivoAsync(request));
    }

    [HttpPost("{id:int}/valor-categoria")]
    public async Task<IActionResult> AgregarValorPorCategoria(int id, [FromBody] ValorCategoriaConfiguradoInputDto request)
    {
        return Ok(await configuracionService.AddValorPorCategoriaAsync(id, request));
    }

    [HttpDelete("{id:int}/valor-categoria/{valorCategoriaId:int}")]
    public async Task<IActionResult> EliminarValorPorCategoria(int id, int valorCategoriaId)
    {
        return Ok(await configuracionService.RemoveValorPorCategoriaAsync(id, valorCategoriaId));
    }

    [HttpPost("{id:int}/clonar")]
    public async Task<IActionResult> ClonarConfiguracion(int id, [FromBody] ClonarConfiguracionDto request)
    {
        var created = await configuracionService.CloneAsync(id, request);
        return CreatedAtAction(nameof(GetConfiguracion), new { id = created.Id }, created);
    }

    [HttpPost("asociacion-masiva-valores-categorias")]
    public async Task<IActionResult> AsociarValoresCategoriasMasivo([FromBody] AsociacionMasivaValoresCategoriasDto request)
    {
        return Ok(await configuracionService.AsociarValoresCategoriasMasivoAsync(request));
    }

    [HttpPost("desasociacion-masiva-valores-categorias")]
    public async Task<IActionResult> DesasociarValoresCategoriasMasivo([FromBody] AsociacionMasivaValoresCategoriasDto request)
    {
        return Ok(await configuracionService.DesasociarValoresCategoriasMasivoAsync(request));
    }
}
