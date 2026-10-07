namespace Nomenclador.Api.Models;

// Fórmulas de liquidación asociadas a un Concepto (USUARIO.FORMULA). Solo lectura por ahora,
// se usan para mostrarlas en el detalle del concepto.
public class FormulaEntity
{
    public virtual int Id { get; set; }

    public virtual string? Condicion { get; set; }

    public virtual string? Accion { get; set; }

    public virtual int? OrdenEjec { get; set; }

    public virtual int? ConceptoId { get; set; }

    public virtual decimal? CondicionInput { get; set; }

    public virtual decimal? AccionInput { get; set; }

    public virtual string? Codigo { get; set; }

    public virtual string? SpName { get; set; }
}
