namespace Nomenclador.Api.Models;

public class ConfiguracionNomencladorEntity
{
    public virtual int Id { get; set; }

    public virtual int NomencladorId { get; set; }

    public virtual int EscalaSalarialId { get; set; }

    public virtual int ZonaId { get; set; }

    public virtual DateOnly FechaInicio { get; set; }

    public virtual DateOnly? FechaFin { get; set; }

    public virtual IList<ConceptoConfiguradoEntity> Conceptos { get; set; } = [];

    public virtual IList<ValorFijoConfiguradoEntity> ValoresFijos { get; set; } = [];

    public virtual IList<ValorCategoriaConfiguradoEntity> ValoresCategorias { get; set; } = [];
}

public class ConceptoConfiguradoEntity
{
    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ConceptoId { get; set; }

    public virtual int Orden { get; set; }

    override public bool Equals(object? obj)
    {
        if (obj is not ConceptoConfiguradoEntity other)
            return false;

        return ConfiguracionNomencladorId == other.ConfiguracionNomencladorId
            && ConceptoId == other.ConceptoId;
    }

    override public int GetHashCode()
    {
        return HashCode.Combine(ConfiguracionNomencladorId, ConceptoId);
    }
}

public class ValorFijoConfiguradoEntity
{

    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ValorFijoId { get; set; }

    override public bool Equals(object? obj)
    {
        if (obj is not ValorFijoConfiguradoEntity other)
            return false;

        return ConfiguracionNomencladorId == other.ConfiguracionNomencladorId
            && ValorFijoId == other.ValorFijoId;
    }
    override public int GetHashCode()
    {
        return HashCode.Combine(ConfiguracionNomencladorId, ValorFijoId);
    }

}

public class ValorCategoriaConfiguradoEntity
{
    public virtual int ConfiguracionNomencladorId { get; set; }

    public virtual int ValorCategoriaId { get; set; }

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
