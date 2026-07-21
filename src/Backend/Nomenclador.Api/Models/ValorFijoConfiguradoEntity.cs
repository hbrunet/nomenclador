namespace Nomenclador.Api.Models;

public class ValorFijoConfiguradoEntity
{
    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ValorFijoId { get; set; }

    public virtual ValorFijoCatalogEntity ValorFijoCatalog { get; set; } = new ValorFijoCatalogEntity();

    override public bool Equals(object? obj)
    {
        if (obj is not ValorFijoConfiguradoEntity other)
            return false;

        return ConfiguracionNomencladorId == other.ConfiguracionNomencladorId
            && ValorFijoId == other.ValorFijoId;
    }

    override public int GetHashCode()
    {
        return HashCode.Combine(ConfiguracionNomencladorId, ValorFijoId);
    }
}
