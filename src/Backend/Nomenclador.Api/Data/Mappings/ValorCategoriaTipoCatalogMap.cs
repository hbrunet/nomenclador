using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaTipoCatalogMap : ClassMap<ValorCategoriaTipoCatalogEntity>
{
    public ValorCategoriaTipoCatalogMap()
    {
        Table("USUARIO.DESC_VALCAT");
        Id(x => x.Id).Column("IDDVCAT").GeneratedBy.Sequence("USUARIO.DESC_VALCAT_SEQ");
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
