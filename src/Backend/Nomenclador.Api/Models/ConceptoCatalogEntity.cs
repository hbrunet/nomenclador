namespace Nomenclador.Api.Models;

public class ConceptoCatalogEntity
{
    public virtual int Id { get; set; }

    public virtual int Codigo { get; set; }

    public virtual int Subcodigo { get; set; }

    public virtual string DescripcionBreve { get; set; } = string.Empty;

    public virtual string Descripcion { get; set; } = string.Empty;
}
