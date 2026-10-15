namespace Nomenclador.Api.Models;

// Primitivas usadas por las fórmulas de liquidación (USUARIO.PRIMITIVA). Solo lectura por ahora.
public class PrimitivaEntity
{
    public virtual int Id { get; set; }

    public virtual string? Nombre { get; set; }

    public virtual string? Descripcion { get; set; }

    public virtual bool EsResultLogico { get; set; }

    public virtual string? Cabecera { get; set; }

    public virtual string? Cuerpo { get; set; }

    public virtual string? Pie { get; set; }
}
