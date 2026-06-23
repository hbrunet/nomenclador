using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("valores-fijos")]
    public async Task<IActionResult> GetValoresFijos()
    {
        return Ok(await catalogRepository.GetValoresFijosAsync());
    }

    [HttpGet("valores-categorias")]
    public async Task<IActionResult> GetValoresCategorias()
    {
        return Ok(await catalogRepository.GetValoresCategoriasAsync());
    }
}
