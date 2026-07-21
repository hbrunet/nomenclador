using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ConceptoCatalogMap : ClassMap<ConceptoCatalogEntity>
{
    public ConceptoCatalogMap()
    {
        Table("USUARIO.CONCEPTO");
        ReadOnly();
        Id(x => x.Id).Column("IDCONCEPTO").GeneratedBy.Assigned();
        Map(x => x.Codigo).Column("CODIGO");
        Map(x => x.Subcodigo).Column("SUBCOD");
        Map(x => x.DescripcionBreve).Column("DESC_BREVE");
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
