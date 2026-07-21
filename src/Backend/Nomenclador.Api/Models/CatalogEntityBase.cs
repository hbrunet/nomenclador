namespace Nomenclador.Api.Models;

public abstract class CatalogEntityBase
{
    public virtual int Id { get; set; }

    public virtual string Descripcion { get; set; } = string.Empty;
}
