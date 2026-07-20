namespace Nomenclador.Api.Models;

public class ValorCategoriaConfiguradoItemEntity 
{
    public virtual int Id { get; set; }
    public virtual int ValorCategoriaId { get; set; }
    public virtual int Numero { get; set; }
    public virtual decimal Importe { get; set; }
}
