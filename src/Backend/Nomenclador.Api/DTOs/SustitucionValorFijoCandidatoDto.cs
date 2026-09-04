namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorFijoCandidatoDto
{
    public int IdValorFijo { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal Valor { get; init; }
}
