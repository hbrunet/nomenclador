namespace Nomenclador.Api.DTOs;

public sealed class ValorFijoCreateDto
{
    public string Descripcion { get; init; } = string.Empty;

    public int IdTipo { get; init; }

    public decimal Valor { get; init; }

    /// <summary>
    /// Si se proporciona, se crea también el ValorFijoConfigurado asociando el nuevo ítem a esa configuración.
    /// </summary>
    public int? ConfiguracionId { get; init; }
}
