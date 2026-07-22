namespace Nomenclador.Api.DTOs;

public sealed class ValorCategoriaCreateUpdateDto
{
    public string Descripcion { get; init; } = string.Empty;
    public int IdTipo { get; init; }
}
