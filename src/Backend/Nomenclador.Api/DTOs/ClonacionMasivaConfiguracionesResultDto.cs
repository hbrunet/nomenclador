namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaConfiguracionesResultDto
{
    public IReadOnlyCollection<ConfiguracionNomencladorDetailDto> Clones { get; init; } = [];

    public int ConfiguracionesCerradas { get; init; }
}
