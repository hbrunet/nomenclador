using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class EscalaSalarialCatalogMap : ClassMap<EscalaSalarialCatalogEntity>
{
    public EscalaSalarialCatalogMap()
    {
        Table("USUARIO.ESCALASALARIAL");
        ReadOnly();
        Id(x => x.Id).Column("IDESCALASAL").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
