namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaConfiguracionesDto
{
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public bool CopiarConceptos { get; init; } = true;

    public bool CopiarValoresFijos { get; init; } = true;

    public bool CopiarValoresCategoria { get; init; } = true;
}
