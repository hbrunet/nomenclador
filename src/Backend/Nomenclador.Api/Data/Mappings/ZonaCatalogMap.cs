using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ZonaCatalogMap : ClassMap<ZonaCatalogEntity>
{
    public ZonaCatalogMap()
    {
        Table("USUARIO.ZONAS");
        ReadOnly();
        Id(x => x.Id).Column("IDZONAS").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
