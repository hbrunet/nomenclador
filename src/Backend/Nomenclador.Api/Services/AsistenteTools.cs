using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Services;

// Funciones de consulta expuestas al modelo (function calling).
public sealed class AsistenteTools(
    ConfiguracionNomencladorService configuracionService,
    CatalogRepository catalogRepository,
    ConceptoRepository conceptoRepository,
    AyudaAplicacionService ayudaAplicacionService)
{
    private const int MaxResultados = 25;
    // Acepta "25/100", "25 / 100", "25-100", "25 100", "25.100" o "código 25 subcódigo 100".
    private static readonly Regex CodigoSubcodigoRegex = new(
        @"^\s*(?:c[oó]d(?:igo)?\.?\s*:?\s*)?(?<codigo>\d+)\s*" +
        @"(?:[/\\\-–—_.,|⁄∕]|\s|,?\s*(?:y\s+)?sub\s*c[oó]d(?:igo)?\.?\s*:?)\s*(?<subcodigo>\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SoloCodigoRegex = new(
        @"^\s*(?:c[oó]d(?:igo)?\.?\s*:?\s*)?(?<codigo>\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public IList<AITool> CrearTools() =>
        typeof(AsistenteTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<DescriptionAttribute>() is not null)
            .Select(method => (AITool)AIFunctionFactory.Create(method, this))
            .ToList();

    // ---------- Lectura ----------

    [Description("Busca en el manual de usuario instrucciones para usar las pantallas de la aplicación. " +
                 "Usar para preguntas de navegación o procedimientos, como crear una configuración, asociar conceptos, " +
                 "clonar valores o entender un campo. No usar para consultar datos actuales; para eso usá las herramientas de consulta correspondientes.")]
    public Task<string> ConsultarAyuda(
        [Description("Tema o tarea concreta sobre la que pregunta el usuario. Incluí la pantalla o el nombre de la operación si se conoce.")] string consulta)
        => ayudaAplicacionService.BuscarAsync(consulta);

    [Description("Devuelve el período activo de liquidación (primer día del mes, formato dd/MM/yyyy).")]
    public async Task<object> ObtenerPeriodoActivo()
    {
        var periodo = await catalogRepository.GetPeriodoActivoAsync();
        return new { hoy = Formatear(DateOnly.FromDateTime(DateTime.Today)), periodoActivo = Formatear(periodo) };
    }

    [Description("Consulta el catálogo de nomencladores y devuelve sus IDs y descripciones. " +
    "Usar para preguntas como '¿qué nomencladores hay?', " +
    "'mostrame los nomencladores' o 'buscá el nomenclador docente'. " +
    "No devuelve configuraciones ni sus vigencias.")]
    public async Task<object> BuscarNomencladores(
        [Description("Parte de la descripción del nomenclador, por ejemplo 'docente'. " +
    "Omitir si el usuario no indicó un nombre. " +
    "No incluir la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetNomencladoresAsync(), texto);

    [Description("Consulta el catalogo de escalas salariales y devuelve sus IDs y descripciones." + 
    "Usar para preguntas como '¿qué escalas salariales hay?', 'mostrame las escalas' o 'buscá la escala docente'.")]
    public async Task<object> BuscarEscalasSalariales([Description("Parte de la descripción de la escala salarial, por ejemplo 'docente'. " +
    "No incluir la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetEscalasAsync(), texto);

    [Description("Consulta el catálogo de zonas y devuelve sus IDs y descripciones. Usar para preguntas como '¿qué zonas hay?', 'mostrame las zonas' o 'buscá la zona 15'.")]
    public async Task<object> BuscarZonas([Description("Parte de la descripción de la zona, por ejemplo '15'. " +
    "Omitir si el usuario no indicó un nombre. " +
    "No incluir la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetZonasAsync(), texto);

    [Description("Consulta las categorías de una escala salarial y devuelve su número y monto. " +
    "Usar para preguntas como '¿qué categorías tiene la escala docente?' o 'mostrame las categorías de la escala docente'. " +
    "Acepta la descripción de la escala directamente. Si devuelve requiereEleccion, preguntale al usuario cuál escala quiere.")]
    public async Task<object> ListarCategoriasDeEscala(
        [Description("Parte de la descripción de la escala salarial, por ejemplo 'SEG. Y DEFENSA 09/2026'. " +
        "Usar cuando el usuario identifica la escala por su nombre.")] string? escalaSalarial = null,
        [Description("Id interno de una escala que el usuario ya eligió en una lista de opciones.")] int? escalaSalarialId = null)
    {
        var escalas = await catalogRepository.GetEscalasAsync();
        CatalogItemDto? seleccionada;

        if (escalaSalarialId is { } id)
        {
            seleccionada = escalas.FirstOrDefault(e => e.Id == id);
            if (seleccionada is null) return $"No existe la escala salarial {id}.";
        }
        else if (string.IsNullOrWhiteSpace(escalaSalarial))
        {
            return "Indicá la descripción de la escala salarial o el Id de una escala que ya hayas elegido.";
        }
        else
        {
            var coincidencias = escalas.Where(e => Contiene(e.Descripcion, escalaSalarial)).ToList();
            var exactas = coincidencias.Where(e => SonIguales(e.Descripcion, escalaSalarial)).ToList();
            var candidatas = exactas.Count > 0 ? exactas : coincidencias;

            if (candidatas.Count == 0)
                return $"No se encontró una escala salarial que coincida con \"{escalaSalarial.Trim()}\".";

            if (candidatas.Count > 1)
            {
                return new
                {
                    requiereEleccion = true,
                    mensaje = "Hay varias escalas coincidentes. Mostrale estas opciones al usuario y preguntale cuál quiere; " +
                              "cuando elija, volvé a llamar a esta herramienta con escalaSalarialId.",
                    total = candidatas.Count,
                    opciones = candidatas.Take(MaxResultados).Select(e => new { e.Id, e.Descripcion }).ToList(),
                };
            }

            seleccionada = candidatas[0];
        }

        var categorias = await catalogRepository.GetCategoriasAsync(seleccionada.Id);
        return new
        {
            escala = new { seleccionada.Id, seleccionada.Descripcion },
            categorias = categorias.Select(c => new { c.Id, c.Numero, c.Descripcion, c.Monto }).ToList(),
        };
    }

    [Description("Busca y lista conceptos por código/subcódigo o por parte de la descripción. " +
    "Usar cuando el usuario pide una lista o quiere identificar posibles coincidencias, no para obtener el detalle completo de un solo concepto.")]
    public async Task<object> BuscarConceptos([Description("Parte de la descripción o código/subcódigo de los conceptos.")] string texto)
    {
        var (conceptos, total) = await conceptoRepository.GetPagedAsync(PrepararBusquedaConcepto(texto), 1, MaxResultados);
        return new
        {
            total,
            items = conceptos.Select(c => new
            {
                c.Id,
                c.Codigo,
                c.Subcodigo,
                c.Descripcion,
                c.DescripcionBreve,
                c.TipoConcepto,
            }).ToList(),
        };
    }

    [Description("Obtiene el detalle completo de un concepto, incluidas sus fórmulas (excepto el campo procedimiento). " +
    "Acepta directamente un código/subcódigo o parte de la descripción. Si encuentra varias coincidencias, devuelve opciones para que el usuario elija.")]
    public async Task<object> ObtenerConcepto(
        [Description("Código/subcódigo, por ejemplo '25/100', o parte de la descripción. Usar cuando el usuario identificó el concepto por esos datos.")] string? concepto = null,
        [Description("Id interno del concepto. Usar solo si el usuario lo indicó explícitamente o eligió un concepto de las opciones devueltas.")] int? conceptoId = null)
    {
        if (conceptoId is { } id)
        {
            var detallePorId = await conceptoRepository.GetByIdAsync(id);
            return (object?)detallePorId ?? $"No existe el concepto con Id {id}.";
        }

        if (string.IsNullOrWhiteSpace(concepto))
            return "Indicá el código/subcódigo, una parte de la descripción o el Id interno del concepto.";

        var busqueda = PrepararBusquedaConcepto(concepto);
        var (encontrados, totalEncontrados) = await conceptoRepository.GetPagedAsync(busqueda, 1, 100);
        // Un código suelto ("25") se busca con LIKE en el repositorio: quedarse con el código exacto.
        var coincidencias = int.TryParse(busqueda, out var codigoExacto)
            ? encontrados.Where(c => c.Codigo == codigoExacto).ToList()
            : encontrados.ToList();
        var total = int.TryParse(busqueda, out _) ? coincidencias.Count : totalEncontrados;
        if (total == 0)
            return $"No se encontró un concepto que coincida con \"{concepto.Trim()}\".";

        var exactas = coincidencias.Where(c =>
            SonIguales(c.Descripcion, concepto) || SonIguales(c.DescripcionBreve, concepto)).ToList();
        if (exactas.Count == 1)
            return (object?)await conceptoRepository.GetByIdAsync(exactas[0].Id)
                ?? $"No existe el concepto con Id {exactas[0].Id}.";

        if (total == 1)
            return (object?)await conceptoRepository.GetByIdAsync(coincidencias.First().Id)
                ?? $"No existe el concepto con Id {coincidencias.First().Id}.";

        var opciones = (exactas.Count > 1 ? exactas : coincidencias).Take(MaxResultados).Select(c => new
        {
            c.Id,
            c.Codigo,
            c.Subcodigo,
            c.Descripcion,
            c.DescripcionBreve,
            c.TipoConcepto,
        }).ToList();

        return new
        {
            requiereEleccion = true,
            mensaje = "Hay varios conceptos coincidentes.",
            total = exactas.Count > 1 ? exactas.Count : total,
            opciones,
        };
    }

    [Description("Consulta valores fijos por Id de tipo y/o descripción." + 
    "Usar para preguntas como '¿existe este valor fijo ...?' o '¿hay un valor fijo con ...?' o '¿cuáles son los valores fijos que coinciden con ...?'")]
    public async Task<object> BuscarValoresFijos([Description("Id de tipo a buscar (opcional).")] int? idTipo, 
    [Description("Texto a buscar (opcional).")] string? texto = null)
    {
        var valores = (await catalogRepository.GetValoresFijosAsync())
            .Where(v => (idTipo == null || v.IdTipo == idTipo) && Contiene(v.Descripcion, texto))
            .ToList();
        return new
        {
            total = valores.Count,
            items = valores.Take(MaxResultados).Select(v => new { v.Id, v.Descripcion, v.Tipo, v.Valor }).ToList(),
        };
    }

    [Description("Consulta valores por categoría por Id de tipo y/o descripción." + 
    "Usar para preguntas como '¿existe este valor por categoría ...?' o '¿hay un valor por categoría con ...?' o '¿cuáles son los valores por categoría que coinciden con ...?'")]
    public async Task<object> BuscarValoresPorCategoria([Description("ID de tipo a buscar (opcional).")] int? IdTipo, 
    [Description("Descripción a buscar (opcional).")] string? descripcion = null)
    {
        var valores = (await catalogRepository.GetValoresCategoriasAsync())
            .Where(v => (IdTipo == null || v.IdTipo == IdTipo) && Contiene(v.Descripcion, descripcion))
            .ToList();
        return new
        {
            total = valores.Count,
            items = valores.Take(MaxResultados).Select(v => new { v.Id, v.Descripcion, v.Tipo }).ToList(),
        };
    }

    [Description("Lista configuraciones con filtros combinables. Resuelve nomenclador por descripción y concepto por código/subcódigo " +
    "sin búsquedas previas. Los filtros de valores son IDs de tipo, no IDs de valores individuales. Devuelve hasta 10 resultados por página.")]
    public async Task<object> BuscarConfiguraciones(
        [Description("Parte de la descripción del nomenclador.")] string? nomenclador = null,
        [Description("Id de escala salarial.")] int? escalaSalarialId = null,
        [Description("Id de zona.")] int? zonaId = null,
        [Description("Fecha MM/yyyy sólo configuraciones vigentes en esa fecha.")] string? vigenteEn = null,
        [Description("Estado: Futura, Activa o Vencida.")] string? estado = null,
        [Description("Código/subcódigo del concepto asociado tal como lo escribió el usuario, completo (ej. '25/100', no solo '25'). No es el Id interno.")] string? concepto = null,
        [Description("Id del tipo de valor fijo que debe tener asociado. Incluye cualquier valor de ese tipo.")] int? valorFijoTipoId = null,
        [Description("Id del tipo de valor por categoría que debe tener asociado. Incluye cualquier valor de ese tipo.")] int? valorCategoriaTipoId = null,
        [Description("Número de página (desde 1).")] int pagina = 1,
        [Description("Id interno del nomenclador si ya se conoce o el usuario eligió una opción.")] int? nomencladorId = null,
        [Description("Id interno del concepto si ya se conoce o el usuario eligió una opción.")] int? conceptoId = null)
    {
        var (resultado, respuesta) = await ConsultarConfiguracionesAsync(
            nomenclador, escalaSalarialId, zonaId, vigenteEn, estado, concepto,
            valorFijoTipoId, valorCategoriaTipoId, pagina, nomencladorId, conceptoId);
        return resultado is null ? respuesta! : ResumirConfiguraciones(resultado);
    }

    [Description("Obtiene una configuración con sus conceptos y valores. Acepta su Id o los mismos filtros que BuscarConfiguraciones, " +
    "sin búsquedas previas. Si encuentra varias, devuelve opciones para que el usuario elija; no elijas una por tu cuenta.")]
    public async Task<object> ObtenerConfiguracion(
        [Description("Id de la configuración si ya se conoce o el usuario eligió una opción. Si se indica, no hacen falta filtros.")] int? configuracionId = null,
        [Description("Parte de la descripción del nomenclador.")] string? nomenclador = null,
        [Description("Id de escala salarial.")] int? escalaSalarialId = null,
        [Description("Id de zona.")] int? zonaId = null,
        [Description("Fecha MM/yyyy de vigencia.")] string? vigenteEn = null,
        [Description("Estado: Futura, Activa o Vencida.")] string? estado = null,
        [Description("Código/subcódigo del concepto asociado tal como lo escribió el usuario, completo (ej. '25/100', no solo '25'). No es el Id interno.")] string? concepto = null,
        [Description("Id del tipo de valor fijo asociado, no el Id de un valor individual.")] int? valorFijoTipoId = null,
        [Description("Id del tipo de valor por categoría asociado, no el Id de un valor individual.")] int? valorCategoriaTipoId = null,
        [Description("Id interno del nomenclador si ya se conoce o el usuario eligió una opción.")] int? nomencladorId = null,
        [Description("Id interno del concepto si ya se conoce o el usuario eligió una opción.")] int? conceptoId = null)
    {
        if (configuracionId is null)
        {
            if (string.IsNullOrWhiteSpace(nomenclador) && nomencladorId is null
                && escalaSalarialId is null && zonaId is null && string.IsNullOrWhiteSpace(vigenteEn)
                && string.IsNullOrWhiteSpace(estado) && string.IsNullOrWhiteSpace(concepto) && conceptoId is null
                && valorFijoTipoId is null && valorCategoriaTipoId is null)
            {
                return "Indicá el Id de la configuración o algún filtro para identificarla.";
            }

            var (resultado, respuesta) = await ConsultarConfiguracionesAsync(
                nomenclador, escalaSalarialId, zonaId, vigenteEn, estado, concepto,
                valorFijoTipoId, valorCategoriaTipoId, 1, nomencladorId, conceptoId);
            if (resultado is null) return respuesta!;
            if (resultado.Total == 0) return "No se encontraron configuraciones con los filtros indicados.";

            if (resultado.Total > 1)
            {
                return new
                {
                    requiereEleccion = true,
                    entidad = "configuracion",
                    mensaje = "Hay varias configuraciones. Mostrale estas opciones al usuario y preguntale cuál quiere. " +
                              "Cuando elija, llamá a ObtenerConfiguracion con configuracionId. " +
                              "Para más opciones, usá BuscarConfiguraciones con los mismos filtros y otra página.",
                    coincidencias = ResumirConfiguraciones(resultado),
                };
            }

            configuracionId = resultado.Items.Single().Id;
        }

        try
        {
            var d = await configuracionService.GetByIdAsync(configuracionId.Value);
            return new
            {
                d.Id,
                d.IdNomenclador,
                Nomenclador = d.NomencladorDescripcion,
                d.IdEscalaSalarial,
                Escala = d.EscalaDescripcion,
                d.IdZona,
                Zona = d.ZonaDescripcion,
                FechaInicio = Formatear(d.FechaInicio),
                FechaFin = d.FechaFin is null ? null : Formatear(d.FechaFin.Value),
                d.Estado,
                Conceptos = d.Conceptos.OrderBy(c => c.Orden)
                    .Select(c => new { c.IdConcepto, c.Codigo, c.Subcodigo, c.Descripcion, c.Orden }).ToList(),
                ValoresFijos = d.ValoresFijos.Select(v => new { v.IdValorFijo, v.Descripcion, v.IdTipo, v.Tipo, v.Valor }).ToList(),
                ValoresCategorias = d.ValoresCategorias.Select(v => new
                {
                    v.IdValorCategoria,
                    v.Descripcion,
                    v.IdTipo,
                    v.Tipo,
                    CantidadItems = v.Items.Count,
                }).ToList(),
            };
        }
        catch (KeyNotFoundException ex)
        {
            return ex.Message;
        }
    }

    // ---------- Helpers ----------

    private async Task<(PagedResult<ConfiguracionNomencladorListItemDto>? Resultado, object? Respuesta)> ConsultarConfiguracionesAsync(
        string? nomenclador, int? escalaSalarialId, int? zonaId, string? vigenteEn, string? estado,
        string? concepto, int? valorFijoTipoId, int? valorCategoriaTipoId, int pagina,
        int? nomencladorId, int? conceptoId)
    {
        DateOnly? fecha = null;
        if (!string.IsNullOrWhiteSpace(vigenteEn))
        {
            if (!DateOnly.TryParseExact(vigenteEn.Trim(), ["MM/yyyy", "yyyy-MM"],
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaConsulta))
            {
                return (null, "La fecha de vigencia debe ser válida y tener formato MM/yyyy o yyyy-MM.");
            }

            fecha = fechaConsulta;
        }

        if (!string.IsNullOrWhiteSpace(estado)
            && estado.Trim().ToUpperInvariant() is not ("FUTURA" or "ACTIVA" or "VENCIDA"))
        {
            return (null, "El estado debe ser Futura, Activa o Vencida.");
        }

        if (!string.IsNullOrWhiteSpace(nomenclador))
        {
            var coincidencias = (await catalogRepository.GetNomencladoresAsync())
                .Where(n => Contiene(n.Descripcion, nomenclador)).ToList();
            var exactas = coincidencias.Where(n => SonIguales(n.Descripcion, nomenclador)).ToList();
            var candidatas = exactas.Count > 0 ? exactas : coincidencias;
            if (candidatas.Count == 0)
                return (null, $"No se encontró un nomenclador que coincida con \"{nomenclador.Trim()}\".");

            if (nomencladorId is { } id)
            {
                if (!candidatas.Any(n => n.Id == id))
                    return (null, $"El nomenclador con Id {id} no coincide con la descripción indicada.");
            }
            else if (candidatas.Count > 1)
            {
                return (null, new
                {
                    requiereEleccion = true,
                    entidad = "nomenclador",
                    mensaje = "Hay varios nomencladores coincidentes. Preguntale al usuario cuál quiere y repetí la consulta " +
                              "con nomencladorId y los demás filtros originales.",
                    total = candidatas.Count,
                    opciones = candidatas.Take(MaxResultados).Select(n => new { n.Id, n.Descripcion }).ToList(),
                });
            }
            else
            {
                nomencladorId = candidatas[0].Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(concepto))
        {
            var codigoSubcodigo = ParsearCodigoSubcodigo(concepto);
            var soloCodigo = SoloCodigoRegex.Match(concepto);
            if (codigoSubcodigo is null && !soloCodigo.Success)
                return (null, "El concepto debe indicarse como código/subcódigo, por ejemplo '25/100'.");

            var codigo = codigoSubcodigo?.Codigo ?? int.Parse(soloCodigo.Groups["codigo"].Value, CultureInfo.InvariantCulture);
            var subcodigo = codigoSubcodigo?.Subcodigo;
            var textoConcepto = subcodigo is null ? codigo.ToString(CultureInfo.InvariantCulture) : $"{codigo}/{subcodigo}";

            if (conceptoId is { } id)
            {
                var detalle = await conceptoRepository.GetByIdAsync(id);
                if (detalle is null || detalle.Codigo != codigo || (subcodigo is not null && detalle.Subcodigo != subcodigo))
                    return (null, $"El concepto con Id {id} no coincide con el concepto {textoConcepto}.");
            }
            else
            {
                var (encontrados, _) = await conceptoRepository.GetPagedAsync(textoConcepto, 1, 100);
                // Sin subcódigo el repositorio busca con LIKE (25 trae 125, 250...): quedarse con el código exacto.
                var coincidencias = encontrados
                    .Where(c => c.Codigo == codigo && (subcodigo is null || c.Subcodigo == subcodigo))
                    .ToList();
                if (coincidencias.Count == 0)
                    return (null, $"No se encontró el concepto {textoConcepto}.");

                if (coincidencias.Count > 1)
                {
                    return (null, new
                    {
                        requiereEleccion = true,
                        entidad = "concepto",
                        mensaje = $"Hay varios conceptos con código {textoConcepto}. Preguntale al usuario cuál quiere y repetí " +
                                  "la consulta con conceptoId y los demás filtros originales.",
                        total = coincidencias.Count,
                        opciones = coincidencias.Take(MaxResultados)
                            .Select(c => new { c.Id, c.Codigo, c.Subcodigo, c.Descripcion }).ToList(),
                    });
                }

                conceptoId = coincidencias[0].Id;
            }
        }

        var resultado = await configuracionService.GetAllAsync(
            nomencladorId, escalaSalarialId, zonaId, fecha, estado, Math.Max(1, pagina), 10,
            conceptoId: conceptoId, valorFijoTipoId: valorFijoTipoId, valorCategoriaTipoId: valorCategoriaTipoId);
        return (resultado, null);
    }

    private static object ResumirConfiguraciones(PagedResult<ConfiguracionNomencladorListItemDto> resultado) => new
    {
        resultado.Total,
        resultado.Page,
        resultado.PageSize,
        items = resultado.Items.Select(i => new
        {
            i.Id,
            Nomenclador = i.NomencladorDescripcion,
            Escala = i.EscalaDescripcion,
            Zona = i.ZonaDescripcion,
            FechaInicio = Formatear(i.FechaInicio),
            FechaFin = i.FechaFin is null ? null : Formatear(i.FechaFin.Value),
            i.Estado,
        }).ToList(),
    };

    private static object Filtrar(IReadOnlyCollection<CatalogItemDto> items, string? texto)
    {
        var filtrados = items.Where(i => Contiene($"{i.Id}-{i.Descripcion}", texto)).ToList();
        return new
        {
            total = filtrados.Count,
            items = filtrados.Take(MaxResultados * 2).Select(i => new { i.Id, i.Descripcion }).ToList(),
        };
    }

    private static bool Contiene(string? valor, string? texto) =>
        string.IsNullOrWhiteSpace(texto)
        || (valor is not null
            && CultureInfo.InvariantCulture.CompareInfo.IndexOf(
                valor, texto.Trim(), CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0);

    private static bool SonIguales(string? valor, string texto) =>
        valor is not null
        && CultureInfo.InvariantCulture.CompareInfo.Compare(
            valor.Trim(), texto.Trim(), CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0;

    private static string PrepararBusquedaConcepto(string texto)
    {
        var busqueda = texto.Trim();
        if (busqueda.StartsWith("desc:", StringComparison.OrdinalIgnoreCase)
            || busqueda.StartsWith("d:", StringComparison.OrdinalIgnoreCase))
        {
            return busqueda;
        }

        if (ParsearCodigoSubcodigo(busqueda) is var (codigo, subcodigo))
            return $"{codigo}/{subcodigo}";

        var soloCodigo = SoloCodigoRegex.Match(busqueda);
        return soloCodigo.Success ? soloCodigo.Groups["codigo"].Value : $"desc:{busqueda}";
    }

    private static (int Codigo, int Subcodigo)? ParsearCodigoSubcodigo(string? texto)
    {
        var match = CodigoSubcodigoRegex.Match(texto ?? string.Empty);
        return match.Success
               && int.TryParse(match.Groups["codigo"].Value, out var codigo)
               && int.TryParse(match.Groups["subcodigo"].Value, out var subcodigo)
            ? (codigo, subcodigo)
            : null;
    }

    private static string Formatear(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
