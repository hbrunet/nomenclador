namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoConfiguradoDto
{
    public int IdValorFijo { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public string Tipo { get; init; } = string.Empty;

    public decimal Valor { get; init; }
}
