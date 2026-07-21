namespace Nomenclador.Api.Models;

public class ValorCategoriaCatalogEntity : CatalogEntityBase
{
    public virtual ValorCategoriaTipoCatalogEntity? Tipo { get; set; }
}
