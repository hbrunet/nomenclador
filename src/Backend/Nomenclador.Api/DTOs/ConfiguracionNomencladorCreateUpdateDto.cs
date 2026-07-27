namespace Nomenclador.Api.DTOs;

public sealed class ConfiguracionNomencladorCreateUpdateDto
{
    public int IdNomenclador { get; init; }

    public int IdEscalaSalarial { get; init; }

    public int? IdZona { get; init; }

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public IReadOnlyCollection<ConceptoConfiguradoInputDto> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoInputDto> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoInputDto> ValoresCategorias { get; init; } = [];
}
