using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class TipoConceptoCatalogMap : ClassMap<TipoConceptoCatalogEntity>
{
    public TipoConceptoCatalogMap()
    {
        Table("USUARIO.TABTIPOCONCEPTO");
        ReadOnly();
        Id(x => x.Id).Column("IDTABTIPOCONC").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
