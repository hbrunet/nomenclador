namespace Nomenclador.Api.Models;

public abstract class CatalogEntityBase
{
    public virtual int Id { get; set; }

    public virtual string Descripcion { get; set; } = string.Empty;
}

public class NomencladorCatalogEntity : CatalogEntityBase;

public class EscalaSalarialCatalogEntity : CatalogEntityBase;

public class ZonaCatalogEntity : CatalogEntityBase;

public class CategoriaCatalogEntity : CatalogEntityBase
{
    public virtual int EscalaSalarialId { get; set; }

    public virtual int Numero { get; set; }
}

public class ConceptoCatalogEntity
{
    public virtual int Id { get; set; }

    public virtual string Codigo { get; set; } = string.Empty;

    public virtual int Subcodigo { get; set; }

    public virtual string DescripcionBreve { get; set; } = string.Empty;

    public virtual string Descripcion { get; set; } = string.Empty;

}

public class ValorFijoCatalogEntity : CatalogEntityBase
{
    public virtual ValorFijoTipoCatalogEntity? Tipo { get; set; }
    public virtual decimal Valor { get; set; }
}

public class ValorFijoTipoCatalogEntity : CatalogEntityBase;

public class ValorCategoriaCatalogEntity : CatalogEntityBase
{
    public virtual ValorCategoriaTipoCatalogEntity? Tipo { get; set; }
}

public class ValorCategoriaTipoCatalogEntity : CatalogEntityBase;

public class ReparticionTipoEmpleoNomencladorCatalogEntity 
{
    public virtual int ReparticionId { get; set; }
    public virtual int TipoEmpleoId { get; set; }
    public virtual int NomencladorId { get; set; }
}