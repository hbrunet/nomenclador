namespace Nomenclador.Api.Models;

public sealed class ConfiguracionNomencladorCloneSource
{
    public required ConfiguracionNomencladorEntity Entity { get; init; }

    public IReadOnlyCollection<ConceptoConfiguradoEntity> Conceptos { get; init; } = [];

    public IReadOnlyCollection<ValorFijoConfiguradoEntity> ValoresFijos { get; init; } = [];

    public IReadOnlyCollection<ValorCategoriaConfiguradoEntity> ValoresCategorias { get; init; } = [];
}