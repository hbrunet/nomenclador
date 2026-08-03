namespace Nomenclador.Api.Models;

public class ConfiguracionNomencladorEntity
{
    public virtual int Id { get; set; }

    public virtual int NomencladorId { get; set; }

    public virtual int EscalaSalarialId { get; set; }

    public virtual int? ZonaId { get; set; }

    public virtual DateOnly FechaInicio { get; set; }

    public virtual DateOnly FechaFin { get; set; } = DateOnly.MaxValue;

    public virtual IList<ConceptoConfiguradoEntity> Conceptos { get; set; } = [];

    public virtual IList<ValorFijoConfiguradoEntity> ValoresFijos { get; set; } = [];

    public virtual IList<ValorCategoriaConfiguradoEntity> ValoresCategorias { get; set; } = [];
}
