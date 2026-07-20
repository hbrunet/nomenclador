using Microsoft.AspNetCore.Mvc;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
public sealed class CatalogsController(CatalogRepository catalogRepository) : ControllerBase
{
    [HttpGet("nomencladores")]
    public async Task<IActionResult> GetNomencladores()
    {
        return Ok(await catalogRepository.GetNomencladoresAsync());
    }

    [HttpGet("escalas")]
    public async Task<IActionResult> GetEscalas()
    {
        return Ok(await catalogRepository.GetEscalasAsync());
    }

    [HttpGet("zonas")]
    public async Task<IActionResult> GetZonas()
    {
        return Ok(await catalogRepository.GetZonasAsync());
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias([FromQuery] int? escalaId)
    {
        return Ok(await catalogRepository.GetCategoriasAsync(escalaId));
    }

    [HttpPut("categorias/montos")]
    public async Task<IActionResult> UpdateCategoriaMontos([FromBody] IReadOnlyCollection<CategoriaMontoUpdateDto> items)
    {
        await catalogRepository.UpdateCategoriaMontosAsync(items);
        return NoContent();
    }

    [HttpGet("valores-fijos")]
    public async Task<IActionResult> GetValoresFijos()
    {
        return Ok(await catalogRepository.GetValoresFijosAsync());
    }

    [HttpGet("valores-fijos/{id:int}/usages")]
    public async Task<IActionResult> GetValorFijoUsages(int id)
    {
        return Ok(await catalogRepository.GetValorFijoUsagesAsync(id));
    }

    [HttpPut("valores-fijos/{id:int}")]
    public async Task<IActionResult> UpdateValorFijo(int id, ValorFijoUpdateDto dto)
    {
        var result = await catalogRepository.UpdateValorFijoAsync(id, dto);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost("valores-fijos")]
    public async Task<IActionResult> CreateValorFijo(ValorFijoCreateDto dto)
    {
        var result = await catalogRepository.CreateValorFijoAsync(dto);
        return CreatedAtAction(nameof(GetValoresFijos), new { }, result);
    }

    [HttpGet("valores-categorias")]
    public async Task<IActionResult> GetValoresCategorias()
    {
        return Ok(await catalogRepository.GetValoresCategoriasAsync());
    }
    
    [HttpGet("valor-categoria-configurado-items/{id:int}")]
    public async Task<IActionResult> GetValorCategoriaConfiguradoItems(int id)
    {
        var result = await catalogRepository.GetValorCategoriaConfiguradoItemsAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPut("valor-categoria-configurado-items/{id:int}")]
    public async Task<IActionResult> UpdateValorCategoriaItems(
        int id,
        [FromBody] IReadOnlyCollection<ValorCategoriaConfiguradoItemDto> items)
    {
        var result = await catalogRepository.UpdateValorCategoriaItemsAsync(id, items);
        return Ok(result);
    }
}
