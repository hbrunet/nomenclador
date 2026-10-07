using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class FormulaMap : ClassMap<FormulaEntity>
{
    public FormulaMap()
    {
        Table("USUARIO.FORMULA");
        Id(x => x.Id).Column("IDFORM").GeneratedBy.Sequence("USUARIO.FORMULA_SEQ");
        Map(x => x.Condicion).Column("CONDICION").Not.Nullable();
        Map(x => x.Detalle).Column("DETALLE");
        Map(x => x.Accion).Column("ACCION").Not.Nullable();
        Map(x => x.OrdenEjec).Column("ORDENEJEC");
        Map(x => x.ConceptoId).Column("IDCONCEPTO");
        Map(x => x.CondicionInput).Column("CONDICION_INPUT").Not.Nullable();
        Map(x => x.AccionInput).Column("ACCION_INPUT").Not.Nullable();
        Map(x => x.Codigo).Column("CODIGO");
        Map(x => x.SpName).Column("SPNAME");
    }
}
