namespace Nomenclador.Api.DTOs;

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

    public IReadOnlyCollection<ConceptoConfiguradoDto> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoDto> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoDto> ValoresCategorias { get; init; } = [];
}
