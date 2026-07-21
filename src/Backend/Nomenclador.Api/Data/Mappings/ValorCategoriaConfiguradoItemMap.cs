using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaConfiguradoItemMap : ClassMap<ValorCategoriaConfiguradoItemEntity>
{
    public ValorCategoriaConfiguradoItemMap()
    {
        Table("USUARIO.VALCAT_CAT");
        Id(x => x.Id).Column("IDVALCATCAT").GeneratedBy.Sequence("USUARIO.VALCAT_CAT_SEQ");
        Map(x => x.ValorCategoriaId).Column("IDVALCAT");
        Map(x => x.Numero).Column("NROCAT");
        Map(x => x.Importe).Column("IMPORTE");
    }
}
