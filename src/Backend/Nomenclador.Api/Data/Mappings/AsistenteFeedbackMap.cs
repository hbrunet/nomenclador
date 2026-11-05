using FluentNHibernate.Mapping;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data.Mappings;

public sealed class AsistenteFeedbackMap : ClassMap<AsistenteFeedbackEntity>
{
    public AsistenteFeedbackMap()
    {
        Table("USUARIO.ASISTENTE_FEEDBACK");
        Id(x => x.Id).Column("ID").GeneratedBy.Sequence("USUARIO.ASISTENTE_FEEDBACK_SEQ");
        Map(x => x.RespuestaId).Column("RESPUESTA_ID").Not.Nullable().Unique();
        Map(x => x.Usuario).Column("USUARIO").Not.Nullable();
        Map(x => x.Util).Column("UTIL").Not.Nullable();
        Map(x => x.Motivo).Column("MOTIVO");
        Map(x => x.Comentario).Column("COMENTARIO");
        Map(x => x.Herramientas).Column("HERRAMIENTAS");
        Map(x => x.ConsultaUsuario).Column("CONSULTA_USUARIO");
        Map(x => x.RespuestaAsistente).Column("RESPUESTA_ASISTENTE");
        Map(x => x.Modelo).Column("MODELO").Not.Nullable();
        Map(x => x.Fecha).Column("FECHA").Not.Nullable();
    }
}
