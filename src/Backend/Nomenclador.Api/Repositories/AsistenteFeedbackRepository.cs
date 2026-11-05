using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Repositories;

public sealed class AsistenteFeedbackRepository(NHibernate.ISession session)
{
    // Upsert por RespuestaId: permite que el usuario cambie el voto de una misma respuesta.
    public async Task CreateOrUpdateAsync(AsistenteFeedbackEntity feedback)
    {
        var existente = await session.QueryOver<AsistenteFeedbackEntity>()
            .Where(x => x.RespuestaId == feedback.RespuestaId)
            .SingleOrDefaultAsync();

        using var tx = session.BeginTransaction();
        if (existente is null)
        {
            await session.SaveAsync(feedback);
        }
        else
        {
            if (!string.Equals(existente.Usuario, feedback.Usuario, StringComparison.Ordinal))
                throw new Nomenclador.Api.Services.ForbiddenException("No podés modificar el feedback de otro usuario.");
            existente.Util = feedback.Util;
            existente.Motivo = feedback.Motivo;
            existente.Comentario = feedback.Comentario;
            existente.Herramientas = feedback.Herramientas;
            existente.ConsultaUsuario = feedback.ConsultaUsuario;
            existente.RespuestaAsistente = feedback.RespuestaAsistente;
            existente.Modelo = feedback.Modelo;
            existente.Fecha = feedback.Fecha;
        }

        await session.FlushAsync();
        await tx.CommitAsync();
    }

    public async Task<AsistenteFeedbackResumenDto> GetResumenAsync()
    {
        var positivos = await session.QueryOver<AsistenteFeedbackEntity>().Where(x => x.Util).RowCountAsync();
        var negativos = await session.QueryOver<AsistenteFeedbackEntity>().Where(x => !x.Util).RowCountAsync();

        // El agrupado por motivo se hace en memoria: son pocas filas (sólo votos negativos
        // con motivo) y QueryOver no tiene una forma simple de expresar un GROUP BY + COUNT
        // tipado sobre una propiedad nullable.
        var negativosConMotivo = await session.QueryOver<AsistenteFeedbackEntity>()
            .Where(x => !x.Util && x.Motivo != null)
            .ListAsync();

        var motivosAgrupados = negativosConMotivo
            .GroupBy(x => x.Motivo!)
            .Select(g => new AsistenteFeedbackMotivoResumenDto { Motivo = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        return new AsistenteFeedbackResumenDto
        {
            TotalVotos = positivos + negativos,
            Positivos = positivos,
            Negativos = negativos,
            MotivosNegativos = motivosAgrupados,
        };
    }
}
