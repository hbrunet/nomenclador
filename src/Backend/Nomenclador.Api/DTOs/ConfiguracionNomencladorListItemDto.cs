namespace Nomenclador.Api.DTOs;

public sealed class ConfiguracionNomencladorListItemDto
{
    public int Id { get; init; }

    public string NomencladorDescripcion { get; init; } = string.Empty;

    public string EscalaDescripcion { get; init; } = string.Empty;

    public string ZonaDescripcion { get; init; } = string.Empty;

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public string Estado { get; init; } = string.Empty;

}
