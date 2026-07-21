using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorFijoConfiguradoMap : ClassMap<ValorFijoConfiguradoEntity>
{
    public ValorFijoConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_VALORUNICO");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ValorFijoId, "IDVALFIJO");
        References(x => x.ValorFijoCatalog)
            .Column("IDVALFIJO").Not.Nullable()
            .ReadOnly();
    }
}
