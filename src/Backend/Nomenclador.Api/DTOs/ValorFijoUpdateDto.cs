namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoUpdateDto
{
    public string Descripcion { get; init; } = string.Empty;

    public decimal Valor { get; init; }
}
