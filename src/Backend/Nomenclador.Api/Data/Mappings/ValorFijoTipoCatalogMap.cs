using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorFijoTipoCatalogMap : ClassMap<ValorFijoTipoCatalogEntity>
{
    public ValorFijoTipoCatalogMap()
    {
        Table("USUARIO.DESC_VALFIJO");
        ReadOnly();
        Id(x => x.Id).Column("IDDVF").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
