namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoCloneDto
{
    public string Descripcion { get; init; } = string.Empty;

    // Se debe informar exactamente uno de los dos: ajuste por coeficiente o importe nuevo.
    public decimal? CoeficienteAjuste { get; init; }

    public decimal? ValorNuevo { get; init; }
}