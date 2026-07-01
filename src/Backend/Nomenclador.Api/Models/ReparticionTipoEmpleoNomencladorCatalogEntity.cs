namespace Nomenclador.Api.Models;

public class ReparticionTipoEmpleoNomencladorCatalogEntity
{
    public virtual int ReparticionId { get; set; }
    public virtual int TipoEmpleoId { get; set; }
    public virtual int NomencladorId { get; set; }

    override public bool Equals(object? obj)
    {
        if (obj is not ReparticionTipoEmpleoNomencladorCatalogEntity other)
            return false;

        return ReparticionId == other.ReparticionId && TipoEmpleoId == other.TipoEmpleoId;
    }

    override public int GetHashCode()
    {
        return HashCode.Combine(ReparticionId, TipoEmpleoId);
    }
}
