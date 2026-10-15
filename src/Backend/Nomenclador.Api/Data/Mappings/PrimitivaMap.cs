using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class PrimitivaMap : ClassMap<PrimitivaEntity>
{
    public PrimitivaMap()
    {
        Table("USUARIO.PRIMITIVA");
        ReadOnly();
        Id(x => x.Id).Column("IDPRIMITIVA").GeneratedBy.Assigned();
        Map(x => x.Nombre).Column("NOMBRE");
        Map(x => x.Descripcion).Column("DESCRIPCION");
        Map(x => x.EsResultLogico).Column("ESRESULTLOGICO");
        Map(x => x.Cabecera).Column("CABECERA");
        Map(x => x.Cuerpo).Column("CUERPO");
        Map(x => x.Pie).Column("PIE");
    }
}
