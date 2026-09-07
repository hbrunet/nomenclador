namespace Nomenclador.Api.DTOs;

public sealed class SustitucionValorCategoriaMatchDto
{
    public int IdTipo { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public bool Encontrado { get; init; }
    // true cuando hay más de un valor del mismo tipo cuya descripción menciona el período
    // buscado: no se puede determinar cuál es el correcto automáticamente; Candidatos trae
    // todas las opciones para que el usuario elija una a mano.
    public bool Ambiguo { get; init; }
    public int? IdValorCategoria { get; init; }
    public string? Descripcion { get; init; }
    public IReadOnlyCollection<SustitucionValorCategoriaCandidatoDto> Candidatos { get; init; } = [];
}
