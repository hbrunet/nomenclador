using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class NomencladorCatalogMap : ClassMap<NomencladorCatalogEntity>
{
    public NomencladorCatalogMap()
    {
        Table("USUARIO.NOMENCLADOR"); 
        ReadOnly();
        Id(x => x.Id).Column("IDNOM").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}

public sealed class EscalaSalarialCatalogMap : ClassMap<EscalaSalarialCatalogEntity>
{
    public EscalaSalarialCatalogMap()
    {
        Table("USUARIO.ESCALASALARIAL");
        ReadOnly();
        Id(x => x.Id).Column("IDESCALASAL").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}

public sealed class ZonaCatalogMap : ClassMap<ZonaCatalogEntity>
{
    public ZonaCatalogMap()
    {
        Table("USUARIO.ZONAS");
        ReadOnly();
        Id(x => x.Id).Column("IDZONAS").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}

public sealed class CategoriaCatalogMap : ClassMap<CategoriaCatalogEntity>
{
    public CategoriaCatalogMap()
    {
        Table("USUARIO.CATEGORIA");
        ReadOnly();
        Id(x => x.Id).Column("IDCATEGORIA").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("CATEGORIA");
        Map(x => x.EscalaSalarialId).Column("IDESCALASAL");
        Map(x => x.Numero).Column("NROCAT");
    }
}

public sealed class ConceptoCatalogMap : ClassMap<ConceptoCatalogEntity>
{
    public ConceptoCatalogMap()
    {
        Table("USUARIO.CONCEPTO");
        ReadOnly();
        Id(x => x.Id).Column("IDCONCEPTO").GeneratedBy.Assigned();
        Map(x => x.Codigo).Column("CODIGO");
        Map(x => x.Subcodigo).Column("SUBCOD");
        Map(x => x.DescripcionBreve).Column("DESC_BREVE");
        Map(x => x.Descripcion).Column("DESCRIPCION");
    }
}

public sealed class ValorFijoCatalogMap : ClassMap<ValorFijoCatalogEntity>
{
    public ValorFijoCatalogMap()
    {
        Table("USUARIO.VALORUNICO");
        ReadOnly();
        Id(x => x.Id).Column("IDVALFIJO").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
        References(x => x.Tipo).Column("IDDVF").Nullable().NotFound.Ignore().ReadOnly();
        Map(x => x.Valor).Column("VALOR");
    }
}

public sealed class ValorCategoriaCatalogMap : ClassMap<ValorCategoriaCatalogEntity>
{
    public ValorCategoriaCatalogMap()
    {
        Table("USUARIO.VALORPORCATEGORIA");
        ReadOnly();
        Id(x => x.Id).Column("IDVALCAT").GeneratedBy.Assigned();
        Map(x => x.Descripcion).Column("DESCRIPCION");
        References(x => x.Tipo).Column("IDDVALCAT").Nullable().NotFound.Ignore().ReadOnly();
    }
}

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
