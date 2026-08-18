using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class GrupoValorCategoriaMap : ClassMap<GrupoValorCategoriaEntity>
{
    public GrupoValorCategoriaMap()
    {
        Table("USUARIO.GRUPOVALCAT");
        Id(x => x.Id).Column("ID").GeneratedBy.Sequence("USUARIO.GRUPOVALCAT_SEQ");
        Map(x => x.Descripcion).Column("DESCRIPCION");
        HasManyToMany(x => x.Tipos)
            .Table("USUARIO.GRUPOVALCAT_TIPO")
            .ParentKeyColumn("IDGRUPO")
            .ChildKeyColumn("IDDVCAT")
            .Cascade.SaveUpdate();
    }
}
