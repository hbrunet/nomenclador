namespace Nomenclador.Api.Models;

public class ValorFijoCatalogEntity : CatalogEntityBase
{
    public virtual ValorFijoTipoCatalogEntity? Tipo { get; set; }
    public virtual decimal Valor { get; set; }
}
