using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class ConceptoCatalogMap : ClassMap<ConceptoCatalogEntity>
{
    public ConceptoCatalogMap()
    {
        Table("USUARIO.CONCEPTO");
        Id(x => x.Id).Column("IDCONCEPTO").GeneratedBy.Sequence("USUARIO.CONCEPTO_SEQ");
        Map(x => x.Codigo).Column("CODIGO");
        Map(x => x.Subcodigo).Column("SUBCOD");
        Map(x => x.DescripcionBreve).Column("DESC_BREVE");
        Map(x => x.Descripcion).Column("DESCRIPCION");
        Map(x => x.AcumulaJubilacion).Column("ACUMULAJUBILACION");
        Map(x => x.AcumulaObraSocial).Column("ACUMULAOSOC");
        Map(x => x.AcumulaRemunerativo).Column("ACUMULAREMUNERATIVO");
        Map(x => x.Basico).Column("BASICO");
        Map(x => x.Bonificable).Column("BONIFICABLE");
        Map(x => x.CalculaTicket).Column("CALCTICKET");
        Map(x => x.CalculaPorPersona).Column("CALCXPERSONA");
        Map(x => x.CalculoMeses).Column("CALMESES");
        Map(x => x.DeduceJubilacion).Column("DEDUCJUB");
        Map(x => x.DeducePension).Column("DEDUCPEN");
        Map(x => x.Especial).Column("ESPECIAL");
        Map(x => x.Ganancia).Column("GANANCIA");
        Map(x => x.IdPartidaPresupuestaria).Column("IDPARTIDA_PRESUP");
        References(x => x.TipoConcepto).Column("IDTABTIPOCONC").Nullable().NotFound.Ignore();
        Map(x => x.ImprimeCantidad).Column("IMPRIMECANTIDAD");
        Map(x => x.LiquidaSiempre).Column("LIQSIEMPRE");
        Map(x => x.ParticipaFondo).Column("PARTFONDO");
        Map(x => x.Ppp).Column("PPP");
        Map(x => x.Reliquidar).Column("RELIQUIDAR");
        HasManyToMany(x => x.TiposLiquidacion)
            .Table("USUARIO.TIPOLIQCONCEPTO")
            .ParentKeyColumn("IDCONCEPTO")
            .ChildKeyColumn("IDTIPOLIQ")
            .Cascade.SaveUpdate();
    }
}
