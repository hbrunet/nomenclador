namespace Nomenclador.Api.Models;

public class ValorCategoriaConfiguradoEntity
{
    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ValorCategoriaId { get; set; }

    public virtual ValorCategoriaCatalogEntity ValorCategoria { get; set; } = new ValorCategoriaCatalogEntity();

    public virtual IList<ValorCategoriaItemConfiguradoEntity> Items { get; set; } = [];

    override public bool Equals(object? obj)
    {
        if (obj is not ValorCategoriaConfiguradoEntity other)
            return false;

        return ConfiguracionNomencladorId == other.ConfiguracionNomencladorId
            && ValorCategoriaId == other.ValorCategoriaId;
    }

    override public int GetHashCode()
    {
        return HashCode.Combine(ConfiguracionNomencladorId, ValorCategoriaId);
    }
}
