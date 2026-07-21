using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class NomencladorCatalogMap : ClassMap<NomencladorCatalogEntity>
{
    public NomencladorCatalogMap()
    {
        Table("USUARIO.NOMENCLADOR"); 
        ReadOnly();
        Id(x => x.Id).Column("IDNOM").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
