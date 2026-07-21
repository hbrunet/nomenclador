using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class CategoriaCatalogMap : ClassMap<CategoriaCatalogEntity>
{
    public CategoriaCatalogMap()
    {
        Table("USUARIO.CATEGORIA");
        Id(x => x.Id).Column("IDCATEGORIA").GeneratedBy.Sequence("USUARIO.CATEGORIA_SEQ");
        Map(x => x.Descripcion).Column("CATEGORIA");
        Map(x => x.EscalaSalarialId).Column("IDESCALASAL");
        Map(x => x.Numero).Column("NROCAT");
        Map(x => x.Monto).Column("MONTO");
        Map(x => x.DescLarga).Column("DESCLARGA");
    }
}
