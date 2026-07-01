namespace Nomenclador.Api.Models;

public class CategoriaCatalogEntity : CatalogEntityBase
{
    public virtual int EscalaSalarialId { get; set; }

    public virtual int Numero { get; set; }

    public virtual decimal Monto { get; set; }

    public virtual string DescLarga { get; set; } = string.Empty;
}
