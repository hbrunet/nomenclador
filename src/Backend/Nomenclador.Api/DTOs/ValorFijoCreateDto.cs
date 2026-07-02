namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoCreateDto
{
    public string Descripcion { get; init; } = string.Empty;

    public int IdTipo { get; init; }

    public decimal Valor { get; init; }
}
