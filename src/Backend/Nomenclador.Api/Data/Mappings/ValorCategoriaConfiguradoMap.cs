using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaConfiguradoMap : ClassMap<ValorCategoriaConfiguradoEntity>
{
    public ValorCategoriaConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_VALPCAT");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ValorCategoriaId, "IDVALCAT");
        References(x => x.ValorCategoria)
            .Column("IDVALCAT").Not.Nullable()
            .ReadOnly();
    }
}
