namespace Nomenclador.Api.Models;

public sealed class ConfiguracionNomencladorEntity
{
    public int Id { get; set; }

    public int NomencladorId { get; set; }

    public int EscalaSalarialId { get; set; }

    public int ZonaId { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public ICollection<ConceptoConfiguradoEntity> Conceptos { get; set; } = [];

    public ICollection<ValorFijoConfiguradoEntity> ValoresFijos { get; set; } = [];

    public ICollection<ValorCategoriaConfiguradoEntity> ValoresCategorias { get; set; } = [];
}

public sealed class ConceptoConfiguradoEntity
{
    public int Id { get; set; }

    public int ConfiguracionNomencladorId { get; set; }

    public int ConceptoId { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; }
}

public sealed class ValorFijoConfiguradoEntity
{
    public int Id { get; set; }

    public int ConfiguracionNomencladorId { get; set; }

    public int ValorFijoId { get; set; }

    public decimal Importe { get; set; }
}

public sealed class ValorCategoriaConfiguradoEntity
{
    public int Id { get; set; }

    public int ConfiguracionNomencladorId { get; set; }

    public int CategoriaId { get; set; }

    public decimal Importe { get; set; }
}
