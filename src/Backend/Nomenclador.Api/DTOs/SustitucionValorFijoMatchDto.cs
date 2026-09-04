namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorFijoMatchDto
{
    public int IdTipo { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public bool Encontrado { get; init; }
    // true cuando hay más de un valor del mismo tipo cuya descripción menciona el período
    // buscado: no se puede determinar cuál es el correcto automáticamente; Candidatos trae
    // todas las opciones para que el usuario elija una a mano.
    public bool Ambiguo { get; init; }
    public int? IdValorFijo { get; init; }
    public string? Descripcion { get; init; }
    public decimal? Valor { get; init; }
    public IReadOnlyCollection<SustitucionValorFijoCandidatoDto> Candidatos { get; init; } = [];
}

