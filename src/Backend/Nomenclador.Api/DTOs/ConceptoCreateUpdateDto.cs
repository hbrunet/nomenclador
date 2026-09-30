namespace Nomenclador.Api.DTOs;

public sealed class ConceptoCreateUpdateDto
{
    public int Codigo { get; init; }

    public int Subcodigo { get; init; }

    public string DescripcionBreve { get; init; } = string.Empty;

    public string Descripcion { get; init; } = string.Empty;

    public bool AcumulaJubilacion { get; init; }

    public bool AcumulaObraSocial { get; init; }

    public bool AcumulaRemunerativo { get; init; }

    public bool Basico { get; init; }

    public bool Bonificable { get; init; }

    public bool CalculaTicket { get; init; }

    public bool CalculaPorPersona { get; init; }

    // Números de mes (1=ene..12=dic) en los que aplica el concepto.
    public IReadOnlyList<int> MesesAplicables { get; init; } = [];

    public bool DeduceJubilacion { get; init; }

    public bool DeducePension { get; init; }

    public bool Especial { get; init; }

    public bool Ganancia { get; init; }

    public int? IdPartidaPresupuestaria { get; init; }

    public int? IdTipoConcepto { get; init; }

    public bool ImprimeCantidad { get; init; }

    public bool LiquidaSiempre { get; init; }

    public bool ParticipaFondo { get; init; }

    public bool Ppp { get; init; }

    public bool Reliquidar { get; init; }

    public IReadOnlyList<int> TiposLiquidacionIds { get; init; } = [];
}
