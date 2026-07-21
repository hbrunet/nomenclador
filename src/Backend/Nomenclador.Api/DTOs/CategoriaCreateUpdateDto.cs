namespace Nomenclador.Api.DTOs;

public sealed class CategoriaCreateUpdateDto
{
    public int Numero { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal Monto { get; init; }
}
