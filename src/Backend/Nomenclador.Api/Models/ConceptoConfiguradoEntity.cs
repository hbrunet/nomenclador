namespace Nomenclador.Api.Models;

public class ConceptoConfiguradoEntity
{
    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ConceptoId { get; set; }

    public virtual int Orden { get; set; }

    public virtual ConceptoCatalogEntity ConceptoCatalog { get; set; } = new ConceptoCatalogEntity();

    override public bool Equals(object? obj)
    {
        if (obj is not ConceptoConfiguradoEntity other)
            return false;

        return ConfiguracionNomencladorId == other.ConfiguracionNomencladorId
            && ConceptoId == other.ConceptoId;
    }

    override public int GetHashCode()
    {
        return HashCode.Combine(ConfiguracionNomencladorId, ConceptoId);
    }
}
