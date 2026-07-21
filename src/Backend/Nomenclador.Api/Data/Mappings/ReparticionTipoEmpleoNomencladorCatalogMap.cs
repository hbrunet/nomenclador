using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ReparticionTipoEmpleoNomencladorCatalogMap : ClassMap<ReparticionTipoEmpleoNomencladorCatalogEntity>
{
    public ReparticionTipoEmpleoNomencladorCatalogMap()
    {
        Table("USUARIO.REPTENOMENCLADOR");
        ReadOnly();
        CompositeId()
            .KeyProperty(x => x.ReparticionId, "IDREP")
            .KeyProperty(x => x.TipoEmpleoId, "IDTE");
        Map(x => x.NomencladorId).Column("IDNOM");
    }
}
