namespace Nomenclador.Api.Models;

public class ConceptoCatalogEntity
{
    public virtual int Id { get; set; }

    public virtual int Codigo { get; set; }

    public virtual int Subcodigo { get; set; }

    public virtual string DescripcionBreve { get; set; } = string.Empty;

    public virtual string Descripcion { get; set; } = string.Empty;

    public virtual bool AcumulaJubilacion { get; set; }

    public virtual bool AcumulaObraSocial { get; set; }

    public virtual bool AcumulaRemunerativo { get; set; }

    public virtual bool Basico { get; set; }

    public virtual bool Bonificable { get; set; }

    public virtual bool CalculaTicket { get; set; }

    public virtual bool CalculaPorPersona { get; set; }

    // Cadena de 12 caracteres (uno por mes, ene..dic); '1' = el concepto aplica ese mes.
    public virtual string? CalculoMeses { get; set; }

    public virtual bool DeduceJubilacion { get; set; }

    public virtual bool DeducePension { get; set; }

    public virtual bool Especial { get; set; }

    public virtual bool Ganancia { get; set; }

    public virtual int? IdPartidaPresupuestaria { get; set; }

    public virtual TipoConceptoCatalogEntity? TipoConcepto { get; set; }

    public virtual bool ImprimeCantidad { get; set; }

    public virtual bool LiquidaSiempre { get; set; }

    public virtual bool ParticipaFondo { get; set; }

    public virtual bool Ppp { get; set; }

    public virtual bool Reliquidar { get; set; }

    public virtual IList<TipoLiquidacionCatalogEntity> TiposLiquidacion { get; set; } = [];
}
