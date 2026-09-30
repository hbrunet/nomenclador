using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class TipoLiquidacionCatalogMap : ClassMap<TipoLiquidacionCatalogEntity>
{
    public TipoLiquidacionCatalogMap()
    {
        Table("USUARIO.TABTIPOLIQUIDACION");
        ReadOnly();
        Id(x => x.Id).Column("IDTIPOLIQUIDACION").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}
