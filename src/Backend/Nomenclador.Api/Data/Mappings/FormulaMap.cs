using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class FormulaMap : ClassMap<FormulaEntity>
{
    public FormulaMap()
    {
        Table("USUARIO.FORMULA");
        Id(x => x.Id).Column("IDFORM").GeneratedBy.Sequence("USUARIO.FORMULA_SEQ");
        Map(x => x.Condicion).Column("CONDICION");
        Map(x => x.Accion).Column("ACCION");
        Map(x => x.OrdenEjec).Column("ORDENEJEC");
        Map(x => x.ConceptoId).Column("IDCONCEPTO");
        Map(x => x.CondicionInput).Column("CONDICION_INPUT");
        Map(x => x.AccionInput).Column("ACCION_INPUT");
        Map(x => x.Codigo).Column("CODIGO");
        Map(x => x.SpName).Column("SPNAME");
    }
}
