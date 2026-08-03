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
        Map(x => x.ZonaId).Column("IDZONAS").Nullable();

        Map(x => x.FechaInicio)
            .Column("FENCHAINICIAL")
            .CustomType<DateOnlyUserType>()
            .Not.Nullable();

        Map(x => x.FechaFin)
            .Column("FECHAFINAL")
            .CustomType<DateOnlyUserType>()
            .Not.Nullable();

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
