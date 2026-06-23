using Nomenclador.Api.Models;

namespace Nomenclador.Api.DTOs;

public class CatalogItemDto
{
    public int Id { get; init; }

    public string Descripcion { get; init; } = string.Empty;
}

public sealed class CategoriaCatalogDto : CatalogItemDto
{
    public int EscalaSalarialId { get; init; }

    public int Numero { get; init; }
}

public sealed class ValorFijoCatalogDto : CatalogItemDto
{
    public string Tipo { get; init; } = string.Empty;
}

public sealed class ConceptoCatalogDto
{
    public int Id { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public int Subcodigo { get; init; }

    public string DescripcionBreve { get; init; } = string.Empty;

    public string Descripcion { get; init; } = string.Empty;
}

public sealed class ConfiguracionNomencladorListItemDto
{
    public int Id { get; init; }

    public string NomencladorDescripcion { get; init; } = string.Empty;

    public string EscalaDescripcion { get; init; } = string.Empty;

    public string ZonaDescripcion { get; init; } = string.Empty;

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public string Estado { get; init; } = string.Empty;

    public int CantidadConceptos { get; init; }

    public int CantidadValoresFijos { get; init; }
}

public sealed class ConfiguracionNomencladorDetailDto
{
    public int Id { get; init; }

    public int IdNomenclador { get; init; }

    public string NomencladorDescripcion { get; init; } = string.Empty;

    public int IdEscalaSalarial { get; init; }

    public string EscalaDescripcion { get; init; } = string.Empty;

    public int IdZona { get; init; }

    public string ZonaDescripcion { get; init; } = string.Empty;

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public string Estado { get; init; } = string.Empty;

    public IReadOnlyCollection<ConceptoConfiguradoViewModel> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoViewModel> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoViewModel> ValoresCategorias { get; init; } = [];
}

public sealed class ConfiguracionNomencladorCreateUpdateDto
{
    public int IdNomenclador { get; init; }

    public int IdEscalaSalarial { get; init; }

    public int IdZona { get; init; }

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public IReadOnlyCollection<ConceptoConfiguradoInputDto> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoInputDto> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoInputDto> ValoresCategorias { get; init; } = [];
}

public sealed class ConceptoConfiguradoInputDto
{
    public int IdConcepto { get; init; }

    public int Orden { get; init; }
}

public sealed class ValorFijoConfiguradoInputDto
{
    public int IdValorFijo { get; init; }
}

public sealed class ValorCategoriaConfiguradoInputDto
{
    public int IdValorCategoria { get; init; }
}

public sealed class ClonarConfiguracionDto
{
    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public bool CopiarConceptos { get; init; } = true;

    public bool CopiarValoresFijos { get; init; } = true;

    public bool CopiarValoresCategoria { get; init; } = true;
}

public sealed class ValidacionConfiguracionResponse
{
    public bool Valida { get; init; }

    public IReadOnlyCollection<ValidationMessageDto> Errores { get; init; } = [];

    public IReadOnlyCollection<ValidationMessageDto> Warnings { get; init; } = [];
}

public sealed class ValidationMessageDto
{
    public string Codigo { get; init; } = string.Empty;

    public string Mensaje { get; init; } = string.Empty;

    public string? Campo { get; init; }
}
