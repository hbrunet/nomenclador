using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ValorCategoriaItemConfiguradoMap : ClassMap<ValorCategoriaItemConfiguradoEntity>
{
    public ValorCategoriaItemConfiguradoMap()
    {
        Table("USUARIO.VALCAT_CAT");
        Id(x => x.Id).Column("IDVALCATCAT").GeneratedBy.Assigned();
        Map(x => x.ValorCategoriaId).Column("IDVALCAT");
        Map(x => x.Numero).Column("NROCAT");
        Map(x => x.Importe).Column("IMPORTE");
    }
}
