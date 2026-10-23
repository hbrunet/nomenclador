using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Nomenclador.Api.DTOs;

namespace Nomenclador.Api.Services;

public sealed class AsistenteService(
    AsistenteChatClientProvider clientProvider,
    AsistenteTools tools,
    IOptions<AsistenteOptions> options,
    ILogger<AsistenteService> logger)
{
    private const int MaxLargoMensaje = 4000;

    public bool Habilitado => options.Value.Habilitado;

    public async IAsyncEnumerable<AsistenteEventoDto> ChatAsync(
        IReadOnlyCollection<AsistenteMensajeDto> mensajes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var innerClient = clientProvider.Client
            ?? throw new InvalidOperationException("El asistente no está configurado (falta Asistente:ApiKey).");

        // Sin "using": disponer el pipeline dispondría también el cliente singleton subyacente.
        var chatClient = new ChatClientBuilder(innerClient)
            .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = 10)
            .Build();

        var historial = new List<ChatMessage> { new(ChatRole.System, BuildSystemPrompt()) };
        historial.AddRange(mensajes
            .Where(m => !string.IsNullOrWhiteSpace(m.Contenido))
            .TakeLast(options.Value.MaxMensajesHistorial)
            .Select(m => new ChatMessage(
                m.Rol == "assistant" ? ChatRole.Assistant : ChatRole.User,
                m.Contenido.Length > MaxLargoMensaje ? m.Contenido[..MaxLargoMensaje] : m.Contenido)));

        var chatOptions = new ChatOptions
        {
            Tools = tools.CrearTools(),
            Temperature = 0.2f,
        };

        var accionesEmitidas = 0;
        var huboTexto = false;

        await using var enumerator = chatClient
            .GetStreamingResponseAsync(historial, chatOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            AsistenteEventoDto? errorEvento = null;
            bool hayMas;
            try
            {
                hayMas = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al consultar el modelo del asistente.");
                errorEvento = new AsistenteEventoDto { Tipo = "error", Texto = DescribirError(ex) };
                hayMas = false;
            }

            if (errorEvento is not null)
            {
                yield return errorEvento;
                yield break;
            }

            if (!hayMas) break;

            var update = enumerator.Current;

            foreach (var llamada in update.Contents.OfType<FunctionCallContent>())
            {
                yield return new AsistenteEventoDto { Tipo = "herramienta", Texto = llamada.Name };
            }

            while (accionesEmitidas < tools.AccionesPropuestas.Count)
            {
                yield return new AsistenteEventoDto { Tipo = "accion", Accion = tools.AccionesPropuestas[accionesEmitidas++] };
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                huboTexto = true;
                yield return new AsistenteEventoDto { Tipo = "texto", Texto = update.Text };
            }
        }

        while (accionesEmitidas < tools.AccionesPropuestas.Count)
        {
            yield return new AsistenteEventoDto { Tipo = "accion", Accion = tools.AccionesPropuestas[accionesEmitidas++] };
        }

        if (!huboTexto && accionesEmitidas == 0)
        {
            logger.LogWarning("El modelo del asistente no devolvió contenido.");
            yield return new AsistenteEventoDto
            {
                Tipo = "error",
                Texto = "El modelo no devolvió ninguna respuesta. Verificá que el servicio de IA esté disponible y probá reformular la consulta.",
            };
        }

        yield return new AsistenteEventoDto { Tipo = "fin" };
    }

    private static string DescribirError(Exception ex) => ex switch
    {
        System.ClientModel.ClientResultException { Status: 401 or 403 } =>
            "El proveedor de IA rechazó las credenciales. Verificá Asistente:ApiKey.",
        System.ClientModel.ClientResultException { Status: 404 } =>
            "El modelo configurado no existe en el proveedor de IA. Verificá Asistente:Model (en Ollama: \"ollama list\").",
        System.ClientModel.ClientResultException { Status: 429 } =>
            "Se alcanzó el límite de uso del modelo de IA. Probá nuevamente en unos minutos.",
        HttpRequestException or System.ClientModel.ClientResultException { InnerException: HttpRequestException } =>
            "No se pudo conectar con el servicio de IA. Verificá que esté en ejecución y que Asistente:Endpoint sea correcto.",
        TaskCanceledException or TimeoutException
            or System.ClientModel.ClientResultException { InnerException: TaskCanceledException or TimeoutException } =>
            "El modelo de IA tardó demasiado en responder. Probá con una consulta más simple.",
        _ => "No se pudo obtener respuesta del asistente. Intentá nuevamente.",
    };

    private static string BuildSystemPrompt()
    {
        var hoy = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"""
            Sos el asistente del "Sistema de configuración del Nomenclador salarial". Respondés siempre en español rioplatense,
            de forma clara y concisa, usando Markdown simple (listas, negritas, tablas cortas) cuando ayude. Hoy es {hoy}.

            ## Dominio
            - Una **configuración de nomenclador** combina Nomenclador + Escala salarial + Zona con un rango de vigencia
              (FechaInicio/FechaFin, siempre primer día de mes; FechaFin vacía = sin fin) y asocia **conceptos** (ordenados),
              **valores fijos** (a lo sumo uno por tipo) y **valores por categoría** (montos por categoría de la escala).
            - Estado según el período activo: **Futura** (empieza después), **Activa** (vigente), **Vencida** (terminó).
            - Regla de superposición: no puede haber dos configuraciones con el mismo Nomenclador + Escala + Zona y vigencias superpuestas.
            - Clonar una configuración copia su combinación y (opcionalmente) conceptos, valores fijos y valores por categoría a un nuevo rango.

            ## Ayuda de uso de la aplicación (menú superior)
            - *Configuraciones*: listado con filtros, alta (botón Nueva), edición, clonación individual y masiva.
            - *Conceptos*: catálogo de conceptos y sus fórmulas.
            - *Escalas Salariales*: escalas, categorías y montos; *Actualización masiva* para pasar configuraciones a una escala nueva.
            - *Valores fijos* / *Valores por categoría*: valores, tipos y grupos.
            - *Asociación masiva*: asociar/desasociar valores fijos, valores por categoría o conceptos a muchas configuraciones.
            - *Clonación masiva*: clonar valores fijos o valores por categoría.

            ## Reglas
            - Usá las herramientas para obtener datos reales; nunca inventes ids, montos, fechas ni descripciones.
            - Para filtrar por nomenclador/escala/zona/concepto/valor primero resolvé el id con la herramienta de búsqueda correspondiente.
              Si hay varias coincidencias ambiguas, preguntá al usuario cuál quiere.
            - Las herramientas "Proponer*" NO modifican datos: sólo generan una propuesta que el usuario confirma con un botón en pantalla.
              Después de proponer, explicá brevemente qué se va a hacer y aclarale que debe confirmarlo. Nunca digas que la acción ya se realizó.
            - Sólo podés proponer: clonar una configuración, agregar/quitar un concepto, asociar/quitar un valor fijo. Para otras modificaciones
              indicá en qué pantalla de la aplicación puede hacerlas.
            - Si un listado tiene muchos resultados, resumí y mencioná el total; ofrecé refinar el filtro.
            - Al mencionar configuraciones incluí su id para que el usuario pueda abrirlas.
            - Si la consulta no tiene relación con el sistema de nomenclador, indicá amablemente que sólo podés ayudar con este sistema.
            """;
    }
}
