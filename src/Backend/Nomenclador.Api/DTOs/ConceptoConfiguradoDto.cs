namespace Nomenclador.Api.DTOs;

public sealed class ConceptoConfiguradoDto
{
    public int IdConcepto { get; init; }

    public string Codigo { get; init; } = string.Empty;

    public int Subcodigo { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public int Orden { get; init; }
    public string DescripcionBreve { get; init; } = string.Empty;
}
