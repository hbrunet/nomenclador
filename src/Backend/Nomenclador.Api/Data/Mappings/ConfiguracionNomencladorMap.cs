using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ConfiguracionNomencladorMap : ClassMap<ConfiguracionNomencladorEntity>
{
    public ConfiguracionNomencladorMap()
    {
        Table("USUARIO.HISTORIALNOMENCLADOR"); 

        Id(x => x.Id)
            .Column("IDHISTORIAL")
            .GeneratedBy.Sequence("USUARIO.HISTORIALNOMENCLADOR_SEQ"); 

        Map(x => x.NomencladorId).Column("IDNOM").Not.Nullable();
        Map(x => x.EscalaSalarialId).Column("IDESCALASAL").Not.Nullable();
        Map(x => x.ZonaId).Column("IDZONAS").Not.Nullable();

        Map(x => x.FechaInicio)
            .Column("FENCHAINICIAL")
            .CustomType<DateOnlyUserType>()
            .Not.Nullable();

        Map(x => x.FechaFin)
            .Column("FECHAFINAL")
            .CustomType<NullableDateOnlyUserType>()
            .Nullable();

        HasMany(x => x.Conceptos)
            .Table("USUARIO.HISTNOM_CONCEPTO")
            .KeyColumn("IDHISTORIAL")
            .Inverse()
            .Cascade.AllDeleteOrphan();
        
        HasMany(x => x.ValoresFijos)
            .Table("USUARIO.HISTNOM_VALORUNICO")
            .KeyColumn("IDHISTORIAL")
            .Inverse()
            .Cascade.AllDeleteOrphan();

        HasMany(x => x.ValoresCategorias)
            .Table("USUARIO.HISTNOM_VALPCAT")
            .KeyColumn("IDHISTORIAL")
            .Inverse()
            .Cascade.AllDeleteOrphan();
    }
}

public sealed class ConceptoConfiguradoMap : ClassMap<ConceptoConfiguradoEntity>
{
    public ConceptoConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_CONCEPTO");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ConceptoId, "IDCONCEPTO");
        Map(x => x.Orden).Column("ORDEN");
    }
}

public sealed class ValorFijoConfiguradoMap : ClassMap<ValorFijoConfiguradoEntity>
{
    public ValorFijoConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_VALORUNICO");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ValorFijoId, "IDVALFIJO");
    }
}

public sealed class ValorCategoriaConfiguradoMap : ClassMap<ValorCategoriaConfiguradoEntity>
{
    public ValorCategoriaConfiguradoMap()
    {
        Table("USUARIO.HISTNOM_VALPCAT");
        CompositeId()
            .KeyProperty(x => x.ConfiguracionNomencladorId, "IDHISTORIAL")
            .KeyProperty(x => x.ValorCategoriaId, "IDVALCAT");
    }
}
