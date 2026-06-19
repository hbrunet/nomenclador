namespace Nomenclador.Api.Models;

public sealed class ConfiguracionNomencladorViewModel
{
    public int Id { get; init; }

    public CatalogItemViewModel Nomenclador { get; init; } = new();

    public CatalogItemViewModel EscalaSalarial { get; init; } = new();

    public CatalogItemViewModel Zona { get; init; } = new();

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public string Estado { get; init; } = string.Empty;

    public IReadOnlyCollection<ConceptoConfiguradoViewModel> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoViewModel> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoViewModel> ValoresCategorias { get; init; } = [];
}

public sealed class ConceptoConfiguradoViewModel
{
    public int IdRelacion { get; init; }

    public int IdConcepto { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public int Subcodigo { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public string Clasificacion { get; init; } = string.Empty;

    public int Orden { get; init; }

    public bool Activo { get; init; }
}

public sealed class ValorFijoConfiguradoViewModel
{
    public int IdRelacion { get; init; }

    public int IdValorFijo { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public string Tipo { get; init; } = string.Empty;

    public decimal Importe { get; init; }
}

public sealed class ValorCategoriaConfiguradoViewModel
{
    public int IdRelacion { get; init; }

    public int IdCategoria { get; init; }

    public string CategoriaDescripcion { get; init; } = string.Empty;

    public int NumeroCategoria { get; init; }

    public decimal Importe { get; init; }
}
