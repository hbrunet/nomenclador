using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ConceptoConfiguradoMap : ClassMap<ConceptoConfiguradoEntity>
{
    public ConceptoConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_CONCEPTO");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ConceptoId, "IDCONCEPTO");
        Map(x => x.Orden).Column("ORDEN");
        References(x => x.ConceptoCatalog)
            .Column("IDCONCEPTO")
            .Not.Nullable()
            .ReadOnly();
    }
}
