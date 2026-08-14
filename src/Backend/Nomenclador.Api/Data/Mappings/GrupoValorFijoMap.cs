using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class GrupoValorFijoMap : ClassMap<GrupoValorFijoEntity>
{
    public GrupoValorFijoMap()
    {
        Table("USUARIO.GRUPOVALFIJO");
        Id(x => x.Id).Column("ID").GeneratedBy.Sequence("USUARIO.GRUPOVALFIJO_SEQ");
        Map(x => x.Descripcion).Column("DESCRIPCION");
        HasManyToMany(x => x.Tipos)
            .Table("USUARIO.GRUPOVALFIJO_TIPO")
            .ParentKeyColumn("IDGRUPO")
            .ChildKeyColumn("IDDVF")
            .Cascade.SaveUpdate();
    }
}
