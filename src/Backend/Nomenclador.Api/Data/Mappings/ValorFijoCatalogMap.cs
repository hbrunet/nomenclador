using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorFijoCatalogMap : ClassMap<ValorFijoCatalogEntity>
{
    public ValorFijoCatalogMap()
    {
        Table("USUARIO.VALORUNICO");
        Id(x => x.Id).Column("IDVALFIJO").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
        References(x => x.Tipo).Column("IDDVF").Nullable().NotFound.Ignore().ReadOnly();
        Map(x => x.Valor).Column("VALOR");
    }
}
