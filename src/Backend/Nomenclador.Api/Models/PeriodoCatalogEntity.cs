namespace Nomenclador.Api.Models;
public class PeriodoCatalogEntity
{
    public virtual DateOnly Periodo { get; set; }
    public virtual string Descripcion { get; set; } = string.Empty;
    public virtual bool Activo { get; set; }
}