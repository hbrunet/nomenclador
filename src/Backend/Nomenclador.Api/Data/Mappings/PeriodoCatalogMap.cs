using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;
namespace Nomenclador.Api.Data.Mappings;

public sealed class PeriodoCatalogMap : ClassMap<PeriodoCatalogEntity>
{
    public PeriodoCatalogMap()
    {
        Table("USUARIO.PERIODO");
        Id(x => x.Periodo)
            .Column("PERIODO")
            .CustomType<DateOnlyUserType>()
            .GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
        Map(x => x.Activo).Column("ACTIVO");
    }
}