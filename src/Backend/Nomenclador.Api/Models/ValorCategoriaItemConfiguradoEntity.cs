namespace Nomenclador.Api.Models;

public class ValorCategoriaItemConfiguradoEntity 
{
    public virtual int Id { get; set; }
    public virtual int ValorCategoriaId { get; set; }
    public virtual int Numero { get; set; }
    public virtual decimal Importe { get; set; }
}
