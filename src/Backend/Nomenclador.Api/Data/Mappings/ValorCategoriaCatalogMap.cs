using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaCatalogMap : ClassMap<ValorCategoriaCatalogEntity>
{
    public ValorCategoriaCatalogMap()
    {
        Table("USUARIO.VALORPORCATEGORIA");
        Id(x => x.Id).Column("IDVALCAT").GeneratedBy.Sequence("USUARIO.VALORPORCATEGORIA_SEQ");
        Map(x => x.Descripcion).Column("DESCRIPCION");
        References(x => x.Tipo).Column("IDDVALCAT").Nullable().NotFound.Ignore();
    }
}
