namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorCategoriaCandidatoDto
{
    public int IdValorCategoria { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int CantidadItems { get; init; }
}
