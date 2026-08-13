namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoCloneDto
{
    public string Descripcion { get; init; } = string.Empty;

    public decimal CoeficienteAjuste { get; init; }
}