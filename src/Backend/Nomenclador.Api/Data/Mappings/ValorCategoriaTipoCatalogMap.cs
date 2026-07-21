using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaTipoCatalogMap : ClassMap<ValorCategoriaTipoCatalogEntity>
{
    public ValorCategoriaTipoCatalogMap()
    {
        Table("USUARIO.DESC_VALCAT");
        ReadOnly();
        Id(x => x.Id).Column("IDDVCAT").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
