using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
    private static readonly Regex IndicadoresConsultaAyudaRegex = new(
        @"\b(como|donde|para que|que significa|que es|ayuda|ayudame|pantalla|opcion|boton|campo|formulario|" +
        @"paso a paso|crear|creacion|alta|editar|modificar|cambiar|eliminar|borrar|asociar|asocio|asociacion|" +
        @"desasociar|clonar|clonacion|copiar|actualizar|actualizacion|funciona|funcionamiento|error|no puedo|" +
        @"explicar|explicame|ensenar|ensename)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool Habilitado => options.Value.Habilitado;

    public async IAsyncEnumerable<AsistenteEventoDto> ChatAsync(
        IReadOnlyCollection<AsistenteMensajeDto> mensajes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mensajeActual = mensajes.LastOrDefault(m => m.Rol != "assistant")?.Contenido;
        if (EsConsultaDeUso(mensajeActual))
        {
            yield return new AsistenteEventoDto { Tipo = "herramienta", Texto = nameof(AsistenteTools.ConsultarAyuda) };
            string? respuestaAyuda = null;
            AsistenteEventoDto? errorAyuda = null;
            try
            {
                respuestaAyuda = await tools.ConsultarAyuda(mensajeActual!);
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
            {
                logger.LogError(ex, "No se pudo consultar el manual de usuario.");
                errorAyuda = new AsistenteEventoDto
                {
                    Tipo = "error",
                    Texto = "No se pudo consultar el manual de usuario. Intentá nuevamente o consultá al administrador.",
                };
            }

            yield return errorAyuda
                ?? new AsistenteEventoDto { Tipo = "texto", Texto = respuestaAyuda };
            yield return new AsistenteEventoDto { Tipo = "fin" };
            yield break;
        }

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
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Asistente llamó a {Herramienta} con {Argumentos}.", llamada.Name,
                        System.Text.Json.JsonSerializer.Serialize(llamada.Arguments));
                }

                yield return new AsistenteEventoDto { Tipo = "herramienta", Texto = llamada.Name };
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                huboTexto = true;
                yield return new AsistenteEventoDto { Tipo = "texto", Texto = update.Text };
            }
        }

        if (!huboTexto)
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

            ## Reglas
            - Usá las herramientas para obtener datos reales; nunca inventes ids, montos, fechas ni descripciones.
            - Obligatorio: para preguntas sobre cómo usar las pantallas, dónde encontrar una opción o cómo completar un
              procedimiento, llamá ConsultarAyuda antes de responder y basá la respuesta en las secciones devueltas.
              En esas consultas no hay otras herramientas disponibles. Si el manual no alcanza, decilo y pedí una
              aclaración; no inventes pasos ni nombres de opciones.
            - Para preguntas sobre datos actuales del sistema, usá las herramientas de consulta, no ConsultarAyuda.
            - Para obtener el detalle de un concepto usá ObtenerConcepto pasando directamente el código/subcódigo o una parte de la descripción.
              Pasá conceptoId sólo si el usuario indicó el Id interno o eligió una opción. Si devuelve requiereEleccion, mostrá las opciones
              y preguntá cuál quiere antes de volver a llamar la herramienta con ese Id.
            - Para listar conceptos usá BuscarConceptos.
            - Para listar configuraciones usá BuscarConfiguraciones; para ver el detalle de una, ObtenerConfiguracion.
              Ambas aceptan nomenclador por descripción, concepto por código/subcódigo y valores fijos o por categoría por Id de tipo,
              sin búsquedas previas. No confundas el Id de tipo con el Id de un valor individual. Escala y zona siguen usando sus IDs.
              Si devuelven requiereEleccion, preguntá cuál quiere y repetí la consulta con el Id de la entidad elegida,
              conservando los demás filtros. Nunca elijas la primera coincidencia por tu cuenta.
            - Para ver categorías de una escala usá ListarCategoriasDeEscala con su descripción. Si devuelve requiereEleccion, preguntá
              cuál escala quiere y volvé a llamar con el Id elegido.
            - Por ahora el asistente es sólo de consulta: no puede crear, modificar, eliminar, clonar ni proponer cambios.
              Si el usuario pide una escritura, explicá que debe realizarla desde la pantalla correspondiente de la aplicación.
            - Si un listado tiene muchos resultados, resumí y mencioná el total; ofrecé refinar el filtro.
            - En toda respuesta sobre registros consultados incluí el Id interno de cada registro o entidad que muestres
              (conceptos, escalas, categorías, nomencladores, zonas, configuraciones y valores), tanto en detalles como en listas
              y opciones para elegir. En tablas agregá una columna Id; en texto o listas indicá "Id: ...".
              No confundas el Id con el código/subcódigo ni con el número de categoría. Usá sólo los Id devueltos por las herramientas;
              si una herramienta no devuelve el Id de una entidad, indicá que no está disponible, sin inventarlo.
            - Si la consulta no tiene relación con el sistema de nomenclador, indicá amablemente que sólo podés ayudar con este sistema.
            """;
    }

    private static bool EsConsultaDeUso(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return false;

        var normalizado = mensaje.Normalize(NormalizationForm.FormD);
        var sinAcentos = string.Concat(normalizado
            .Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC);
        return IndicadoresConsultaAyudaRegex.IsMatch(sinAcentos);
    }
}
