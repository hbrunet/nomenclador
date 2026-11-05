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
    private const string VerbosAccionAyuda =
        @"(?:us\w*|utiliz\w*|busc\w*|filtr\w*|cre\w*|edit\w*|cambi\w*|modific\w*|configur\w*|" +
        @"seleccion\w*|complet\w*|agreg\w*|quit\w*|asoci\w*|clon\w*|acced\w*|ingres\w*|naveg\w*|" +
        @"consult\w*|muestr\w*|mostr\w*|abr\w*|encuentr\w*|ve\w*|hac\w*|hago|elimin\w*|borr\w*|actualiz\w*)";
    private static readonly Regex IndicadoresConsultaAyudaRegex = new(
        @"\b(?:" +
        @"como\s+(?:(?:se\s+puede|se|puede)\s+)?" + VerbosAccionAyuda + @"\b|como\s+(?:puedo|hago)\b|" +
        @"donde\s+(?:(?:esta|encuentro|queda|accedo|ingreso|aparece)|" +
        @"(?:(?:puedo|debo|se\s+puede|se)\s+)?" + VerbosAccionAyuda + @")|" +
        @"en\s+que\s+(?:pantalla|menu|pestana|boton|campo|seccion)\b|" +
        @"(?:que|cual)\s+(?:boton|menu|pantalla|pestana|campo)\b|" +
        @"(?:manual|guia|tutorial)\s*(?:de|para|sobre)?|" +
        @"(?:instrucciones|pasos|paso a paso|procedimiento)\s*(?:de|para|sobre)?|" +
        @"(?:necesito|quiero|busco)\s+(?:saber\s+)?(?:como|donde|ayuda)|" +
        @"(?:necesito|quiero|busco)\s+ayuda(?:\s+(?:para|con|a))?|" +
        @"ayudame\s+(?:a|con|para)|" +
        @"que\s+(?:hago\s+(?:para|si|cuando)|pasos\s+(?:sigo\s+)?(?:para|de|sobre))|" +
        @"(?:puedo|debo|tengo que)\s+(?:crear|editar|modificar|asociar|desasociar|clonar|actualizar|eliminar)|" +
        @"no\s+(?:puedo|logro|consigo)\s+\w+|" +
        @"no\s+(?:encuentro|veo|aparece|me deja)\b|" +
        @"(?:uso|utilizo)\s+(?:del|de la|de|el|la)\s+(?:sistema|aplicacion|pantalla|menu|asistente)|" +
        @"explicame\s+(?:como|el uso|la pantalla|el menu|el procedimiento|los pasos|el login)|" +
        @"(?:instrucciones|pasos|procedimiento)\s+de\s+uso)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool Habilitado => options.Value.Habilitado;

    public async IAsyncEnumerable<AsistenteEventoDto> ChatAsync(
        IReadOnlyCollection<AsistenteMensajeDto> mensajes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mensajeActual = mensajes.LastOrDefault(m => m.Rol == "user")?.Contenido;
        string? ayuda = null;
        if (mensajeActual is not null && EsConsultaDeUso(mensajeActual))
        {
            yield return new AsistenteEventoDto { Tipo = "herramienta", Texto = nameof(AsistenteTools.ConsultarAyuda) };
            AsistenteEventoDto? errorAyuda = null;
            try
            {
                ayuda = await tools.ConsultarAyuda(mensajeActual);
                logger.LogInformation("El asistente consultó automáticamente el manual de usuario.");
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

            if (errorAyuda is not null)
            {
                yield return errorAyuda;
                yield return new AsistenteEventoDto { Tipo = "fin" };
                yield break;
            }
        }

        var innerClient = clientProvider.Client
            ?? throw new InvalidOperationException("El asistente no está configurado (falta Asistente:ApiKey).");

        // Sin "using": disponer el pipeline dispondría también el cliente singleton subyacente.
        var chatClient = new ChatClientBuilder(innerClient)
            .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = 10)
            .Build();

        var historial = new List<ChatMessage> { new(ChatRole.System, BuildSystemPrompt()) };
        if (ayuda is not null)
        {
            historial.Add(new ChatMessage(ChatRole.System, $"""
                El servidor ya ejecutó ConsultarAyuda para la consulta actual. Basá las instrucciones de uso en el
                siguiente resultado del manual; no hace falta volver a llamar ConsultarAyuda para el mismo tema.
                Si el manual no alcanza, pedí una aclaración sin inventar pasos.
                Si el usuario también solicita datos reales, consultalos con las herramientas de datos disponibles.

                <manual_usuario>
                {ayuda}
                </manual_usuario>
                """));
        }

        historial.AddRange(mensajes
            .Where(m => !string.IsNullOrWhiteSpace(m.Contenido))
            .TakeLast(options.Value.MaxMensajesHistorial)
            .Select(m => new ChatMessage(
                m.Rol == "assistant" ? ChatRole.Assistant : ChatRole.User,
                m.Contenido.Length > MaxLargoMensaje ? m.Contenido[..MaxLargoMensaje] : m.Contenido)));

        tools.ConfigurarEstadoSolicitado(mensajeActual);

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
        System.ClientModel.ClientResultException { Status: 502 or 503 or 504 } =>
            "El servicio de IA no está disponible en este momento (puede estar reiniciándose o cargando el modelo). Probá nuevamente en unos minutos.",
        HttpRequestException or IOException
            or System.ClientModel.ClientResultException { InnerException: HttpRequestException or IOException } =>
            "No se pudo conectar con el servicio de IA. Verificá que esté en ejecución y que Asistente:Endpoint sea correcto.",
        TaskCanceledException or TimeoutException
            or System.ClientModel.ClientResultException { InnerException: TaskCanceledException or TimeoutException } =>
            "El modelo de IA tardó demasiado en responder. Probá con una consulta más simple.",
        _ => "No se pudo obtener respuesta del asistente. Intentá nuevamente.",
    };

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

            ## Enrutamiento de consultas
            - **Ayuda de uso**: preguntas sobre cómo realizar una operación, dónde está una opción, qué pantalla/campo/botón usar
              o por qué no se puede completar una tarea. Consultá el manual y respondé basándote en él.
            - **Datos del sistema**: pedidos de listas, registros, detalles, IDs, estados, períodos o montos actuales. Consultá
              las herramientas de datos; no respondas con el manual ni deduzcas datos a partir de él.
            - **Consulta mixta**: si pide instrucciones y datos concretos, consultá el manual y también las herramientas de datos.
              No consideres que recuperar una sección del manual satisface el pedido de datos.
            - **Intención ambigua**: no elijas una operación ni un registro por el usuario; pedí la aclaración necesaria.

            ## Reglas
            - Usá las herramientas para obtener datos reales; nunca inventes ids, montos, fechas ni descripciones.
            - Obligatorio: para preguntas sobre cómo usar las pantallas, dónde encontrar una opción o cómo completar un
              procedimiento, usá el manual proporcionado por el servidor o llamá ConsultarAyuda antes de responder
              y basá la respuesta en las secciones devueltas.
              Si el manual no alcanza, decilo y pedí una aclaración; no inventes pasos ni nombres de opciones.
            - Para preguntas sobre datos actuales del sistema, usá las herramientas de consulta, no ConsultarAyuda.
              Si la consulta combina ambos propósitos, usá también las herramientas de consulta.
            - Elegí la herramienta de conceptos según el dato solicitado, no según si se indicó un código o una descripción:
              * Para buscar, listar, conocer el nombre o comprobar si existe un concepto por código o descripción, usá BuscarConceptos. Por ejemplo:
                "¿existe el concepto 25/100?" o "listá conceptos de antigüedad". Mostrá las coincidencias sin pedir una elección
                si sólo se solicitó un listado. Si no indicó un filtro, omití texto. Un código solo puede devolver códigos que lo
                contienen: verificá el código exacto antes de afirmar que existe.
              * Para consultar por Id interno o pedir fórmulas, meses aplicables, tipos de liquidación u otras propiedades del detalle
                de un concepto, usá ObtenerConcepto. Por ejemplo: "¿cuál es la fórmula del concepto 25/100?".
                Pasá el código o descripción directamente, sin llamar primero a BuscarConceptos. Usá conceptoId sólo si el usuario
                indicó el Id interno o eligió una opción. Si devuelve requiereEleccion, mostrá las opciones y preguntá cuál quiere
                antes de volver a llamar la herramienta con ese Id.
            - Para encontrar por descripción un tipo de valor fijo o por categoría, usá BuscarTiposValoresFijos
              o BuscarTiposValoresPorCategoria, respectivamente. Si se pidió filtrar valores individuales por un tipo nombrado,
              resolvé primero su Id y pasalo a BuscarValoresFijos o BuscarValoresPorCategoria. Para configuraciones, pasalo a
              valorFijoTipoId o valorCategoriaTipoId de BuscarConfiguraciones u ObtenerConfiguracion. Si hay varios tipos posibles,
              pedí al usuario que elija antes de filtrar. No confundas tipos con valores individuales.
            - Para buscar, contar o listar configuraciones usá BuscarConfiguraciones; para consultar una por Id o ver sus conceptos
              y valores asociados, usá ObtenerConfiguracion. El Id de configuración hace que se ignoren los demás filtros.
              Ambas aceptan nomenclador por descripción, concepto por código/subcódigo y valores fijos o por categoría por Id de tipo,
              sin búsquedas previas. No confundas el Id de tipo con el Id de un valor individual. Escala y zona siguen usando sus IDs.
              Si devuelven requiereEleccion, preguntá cuál quiere y repetí la consulta con el Id de la entidad elegida,
              conservando los demás filtros. Nunca elijas la primera coincidencia por tu cuenta.
              Combiná TODOS los filtros solicitados: "configuraciones activas con valor fijo de tipo 65" exige
              estado="Activa" Y valorFijoTipoId=65. Para futuras usá estado="Futura"; para vencidas, estado="Vencida".
              Si pide vigentes en una fecha, usá vigenteEn; no agregues estado="Activa" salvo que también lo solicite.
              Revisá filtrosAplicados en la respuesta: un filtro nulo o ausente NO se aplicó; estado="Todos" incluye todos los estados.
              Si falta uno solicitado,
              repetí la consulta incluyéndolo antes de responder. Nunca presentes un total de todos los estados como si fuera de activas.
            - Para ver categorías de una escala usá ListarCategoriasDeEscala con su descripción. Si devuelve requiereEleccion, preguntá
              cuál escala quiere y volvé a llamar con el Id elegido. El monto de una categoría de la escala no es el monto de un valor
              por categoría: las herramientas de consulta de esos valores no devuelven sus importes por categoría.
            - Por ahora el asistente es sólo de consulta: no puede crear, modificar, eliminar, clonar ni proponer cambios.
              Si el usuario pide una escritura, explicá que debe realizarla desde la pantalla correspondiente de la aplicación.
            - Si un listado tiene muchos resultados, resumí y mencioná el total; ofrecé refinar el filtro.
            - En toda respuesta sobre registros consultados incluí el Id interno de cada registro o entidad que muestres
              (conceptos, escalas, categorías, nomencladores, zonas, configuraciones y valores), tanto en detalles como en listas
              y opciones para elegir. En tablas agregá una columna Id; en texto o listas indicá "Id: ...".
              No confundas el Id con el código/subcódigo ni con el número de categoría. Usá sólo los Id devueltos por las herramientas;
              si una herramienta no devuelve el Id de una entidad, indicá que no está disponible, sin inventarlo.
            - Si la consulta no tiene relación con el sistema de nomenclador, indicá amablemente que sólo podés ayudar con este sistema.
            - Siempre verificá que los filtros y parámetros utilizados sean correctos antes de llamar a una herramienta, para evitar resultados incorrectos o confusos.
            - Siempre mostrá el Id de cada registro o entidad en las respuestas, tanto en detalles como en listas y opciones para elegir.
            - Cuando necesites conocer o informar el período activo, llamá a ObtenerPeriodoActivo y usá el campo periodoActivo de su respuesta.
                No lo deduzcas de la fecha de hoy ni inventes un valor.
                Si no podés obtenerlo, informalo.
            - Para buscar configuraciones por estado, usá BuscarConfiguraciones con el filtro estado correspondiente. Si además te piden indicar el período activo,
                consultá también ObtenerPeriodoActivo.
            - Los importes y montos se deben mostrar con el formato numérico adecuado, incluyendo separadores de miles y decimales según la convención local (Argentina).
            """;
    }
}
