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
    AyudaAplicacionService ayudaAplicacionService,
    ILogger<AsistenteTools> logger)
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
    private string? estadoSolicitadoPorUsuario;

    public IList<AITool> CrearTools() =>
        typeof(AsistenteTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<DescriptionAttribute>() is not null)
            .Select(method => (AITool)AIFunctionFactory.Create(method, this))
            .ToList();

    internal void ConfigurarEstadoSolicitado(string? consultaUsuario)
    {
        estadoSolicitadoPorUsuario = InferirEstadoSolicitado(consultaUsuario);
    }

    // ---------- Lectura ----------

    [Description("Busca en el manual de usuario instrucciones para usar las pantallas de la aplicación. " +
                 "Usar para preguntas de navegación, opciones de pantalla, campos, errores o procedimientos, como crear una configuración, " +
                 "asociar conceptos o clonar valores. No usar para consultar listas, registros, estados o montos actuales. " +
                 "Si la pregunta también pide datos actuales, usá además la herramienta de datos correspondiente.")]
    public Task<string> ConsultarAyuda(
        [Description("Tema o tarea concreta sobre la que pregunta el usuario. Incluí la pantalla o el nombre de la operación si se conoce.")] string consulta)
        => ayudaAplicacionService.BuscarAsync(consulta);

    [Description("Devuelve el período activo de liquidación (primer día del mes, formato dd/MM/yyyy). " +
                 "Usar cuando pregunten cuál es el período activo actual; para buscar configuraciones por estado o vigencia, usar BuscarConfiguraciones u ObtenerConfiguracion.")]
    public async Task<object> ObtenerPeriodoActivo()
    {
        var periodo = await catalogRepository.GetPeriodoActivoAsync();
        return new { hoy = Formatear(DateOnly.FromDateTime(DateTime.Today)), periodoActivo = Formatear(periodo) };
    }

    [Description("Lista nomencladores del catálogo, con sus IDs y descripciones. " +
    "Devuelve el total y hasta 50 coincidencias. Para buscar configuraciones o sus vigencias, usá BuscarConfiguraciones.")]
    public async Task<object> BuscarNomencladores(
        [Description("Fragmento del nombre o del Id del nomenclador, por ejemplo 'docente'. " +
        "Busca coincidencias parciales; omití el parámetro para listar sin filtro. No incluyas la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetNomencladoresAsync(), texto);

    [Description("Lista escalas salariales del catálogo, con sus IDs y descripciones. Devuelve el total y hasta 50 coincidencias. " +
    "No devuelve categorías: para verlas usá ListarCategoriasDeEscala; para configuraciones usá BuscarConfiguraciones.")]
    public async Task<object> BuscarEscalasSalariales(
        [Description("Fragmento del nombre o del Id de la escala, por ejemplo 'docente'. " +
        "Busca coincidencias parciales; omití el parámetro para listar sin filtro. No incluyas la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetEscalasAsync(), texto);

    [Description("Lista zonas del catálogo, con sus IDs y descripciones. Devuelve el total y hasta 50 coincidencias. " +
    "No devuelve configuraciones de esas zonas; para eso usá BuscarConfiguraciones.")]
    public async Task<object> BuscarZonas(
        [Description("Fragmento del nombre o del Id de la zona, por ejemplo '15'. " +
        "Busca coincidencias parciales; omití el parámetro para listar sin filtro. No incluyas la pregunta completa.")] string? texto = null)
        => Filtrar(await catalogRepository.GetZonasAsync(), texto);

    [Description("Busca tipos de valores fijos por descripción y devuelve el total y hasta 25 IDs y nombres, no valores individuales. " +
                 "Usá el Id de tipo para filtrar BuscarValoresFijos, BuscarConfiguraciones u ObtenerConfiguracion. " +
                 "Si hay varios tipos posibles, pedí una elección antes de filtrar.")]
    public async Task<object> BuscarTiposValoresFijos(
        [Description("Fragmento de la descripción del tipo de valor fijo, no la descripción de un valor individual.")] string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return "Indicá una parte de la descripción del tipo de valor fijo.";

        return FiltrarPorDescripcion(await catalogRepository.GetValorFijoTiposAsync(), descripcion);
    }

    [Description("Busca tipos de valores por categoría por descripción y devuelve el total y hasta 25 IDs y nombres, no valores individuales. " +
                 "Usá el Id de tipo para filtrar BuscarValoresPorCategoria, BuscarConfiguraciones u ObtenerConfiguracion. " +
                 "Si hay varios tipos posibles, pedí una elección antes de filtrar.")]
    public async Task<object> BuscarTiposValoresPorCategoria(
        [Description("Fragmento de la descripción del tipo de valor por categoría, no la descripción de un valor individual.")] string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return "Indicá una parte de la descripción del tipo de valor por categoría.";

        return FiltrarPorDescripcion(await catalogRepository.GetValorCategoriaTiposAsync(), descripcion);
    }

    [Description("Lista las categorías propias de una escala salarial y devuelve sus IDs, números, descripciones y montos. " +
    "Acepta directamente el nombre o Id de la escala. Si devuelve requiereEleccion, preguntá cuál quiere y repetí con el Id elegido. " +
    "No lista valores por categoría: usá BuscarValoresPorCategoria para el catálogo u ObtenerConfiguracion para los asociados a una configuración.")]
    public async Task<object> ListarCategoriasDeEscala(
        [Description("Parte de la descripción de la escala salarial, por ejemplo 'SEG. Y DEFENSA 09/2026'. " +
        "Usar cuando el usuario identifica la escala por su nombre.")] string? escalaSalarial = null,
        [Description("Id interno de la escala indicado o elegido por el usuario. Si se proporciona, se ignora escalaSalarial.")] int? escalaSalarialId = null)
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

    [Description("Busca o lista conceptos del catálogo por código o descripción, incluso para comprobar si existe uno concreto. " +
    "Devuelve el total y hasta 25 resultados resumidos: Id, código, subcódigo, descripciones y tipo. " +
    "No devuelve fórmulas ni otras propiedades del detalle; para esas consultas o para buscar por Id interno, usá ObtenerConcepto directamente.")]
    public async Task<object> BuscarConceptos(
        [Description("Código, código/subcódigo (por ejemplo '25/100') o fragmento de la descripción; omitilo para listar sin filtro. " +
        "No es el Id interno ni la pregunta completa. " +
        "Un código solo busca coincidencias parciales: verificá el código exacto antes de afirmar que existe. " +
        "Código/subcódigo busca ambos números exactos. Para una descripción solo numérica, usá el prefijo 'desc:', por ejemplo 'desc:25'.")] string? texto = null)
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

    [Description("Obtiene el detalle de un solo concepto: propiedades, meses aplicables, tipos de liquidación y fórmulas, " +
    "sin el contenido del procedimiento almacenado. Usá esta herramienta para esos datos o para consultar por Id interno; " +
    "para listar coincidencias o comprobar existencia por código o descripción usá BuscarConceptos. " +
    "Acepta código, código/subcódigo, descripción o Id directamente, sin búsqueda previa. Si devuelve requiereEleccion, pedí al usuario que elija.")]
    public async Task<object> ObtenerConcepto(
        [Description("Código, código/subcódigo (por ejemplo '25/100') o fragmento de la descripción. " +
        "Para una descripción solo numérica usá 'desc:', por ejemplo 'desc:25'. No es el Id interno.")] string? concepto = null,
        [Description("Id interno indicado o elegido por el usuario. Si se proporciona, se ignora concepto.")] int? conceptoId = null)
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

    [Description("Lista valores fijos individuales del catálogo por Id de tipo y/o descripción del valor. " +
    "Devuelve el total y hasta 25 valores con Id, descripción, tipo e importe. " +
    "Si se indicó el tipo por nombre, resolvé su Id con BuscarTiposValoresFijos antes de filtrar.")]
    public async Task<object> BuscarValoresFijos(
        [Description("Id del tipo de valor fijo, no Id de un valor individual. Omitilo solo si no se pidió filtrar por tipo.")] int? idTipo = null,
        [Description("Fragmento de la descripción del valor fijo individual. Omitilo si no se indicó.")] string? texto = null)
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

    [Description("Lista valores individuales por categoría del catálogo por Id de tipo y/o descripción del valor. " +
    "Devuelve el total y hasta 25 valores con Id, descripción y tipo; no devuelve los montos por categoría. " +
    "Si se indicó el tipo por nombre, resolvé su Id con BuscarTiposValoresPorCategoria antes de filtrar.")]
    public async Task<object> BuscarValoresPorCategoria(
        [Description("Id del tipo de valor por categoría, no Id de un valor individual. Omitilo solo si no se pidió filtrar por tipo.")] int? IdTipo = null,
        [Description("Fragmento de la descripción del valor por categoría individual. Omitilo si no se indicó.")] string? descripcion = null)
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

    [Description("Busca, cuenta o lista configuraciones con filtros combinables. Devuelve el total, filtrosAplicados y hasta 10 resúmenes por página, " +
    "sin conceptos ni valores asociados. Para el detalle de una configuración o consultarla por su Id usá ObtenerConfiguracion. " +
    "Acepta nomenclador por descripción y concepto por código sin búsquedas previas. Combiná todos los filtros pedidos: " +
    "'activas con valor fijo de tipo 65' requiere estado='Activa' y valorFijoTipoId=65. Los filtros de valores son IDs de tipo. " +
    "Si devuelve requiereEleccion, preguntá cuál opción quiere y repetí conservando los demás filtros.")]
    public async Task<object> BuscarConfiguraciones(
        [Description("Parte de la descripción del nomenclador.")] string? nomenclador = null,
        [Description("Id de escala salarial.")] int? escalaSalarialId = null,
        [Description("Id de zona.")] int? zonaId = null,
        [Description("Período MM/yyyy o yyyy-MM para filtrar configuraciones vigentes en ese mes.")] string? vigenteEn = null,
        [Description("Usá 'Activa' si pide activas o vigentes sin una fecha, 'Futura' para futuras y 'Vencida' para vencidas. " +
                     "Para vigencia en un mes no agregues 'Activa' salvo que también lo solicite. Sin este parámetro se incluyen todos los estados.")] string? estado = null,
        [Description("Código o código/subcódigo del concepto asociado. Si indicó subcódigo, incluí ambos (ej. '25/100'). No es el Id interno ni la descripción.")] string? concepto = null,
        [Description("Id del tipo de valor fijo que debe tener asociado. Incluye cualquier valor de ese tipo.")] int? valorFijoTipoId = null,
        [Description("Id del tipo de valor por categoría que debe tener asociado. Incluye cualquier valor de ese tipo.")] int? valorCategoriaTipoId = null,
        [Description("Número de página (desde 1).")] int pagina = 1,
        [Description("Id interno del nomenclador si ya se conoce o el usuario eligió una opción.")] int? nomencladorId = null,
        [Description("Id interno del concepto si ya se conoce o el usuario eligió una opción.")] int? conceptoId = null)
    {
        var estadoDelModelo = estado;
        estado = AplicarEstadoSolicitado(estado);
        RegistrarEstadoAplicado(nameof(BuscarConfiguraciones), estadoDelModelo, estado);

        var (_, respuesta) = await ConsultarConfiguracionesAsync(
            nomenclador, escalaSalarialId, zonaId, vigenteEn, estado, concepto,
            valorFijoTipoId, valorCategoriaTipoId, pagina, nomencladorId, conceptoId);
        return respuesta!;
    }

    [Description("Obtiene el detalle de una configuración, incluidos sus conceptos y valores asociados. " +
    "Los valores por categoría incluyen la cantidad de ítems, no sus montos por categoría. " +
    "Acepta su Id o los mismos filtros que BuscarConfiguraciones; si devuelve requiereEleccion, pedí al usuario que elija. " +
    "Para buscar, contar o listar configuraciones usá BuscarConfiguraciones.")]
    public async Task<object> ObtenerConfiguracion(
        [Description("Id de la configuración indicado o elegido por el usuario. Si se proporciona, se ignoran los demás filtros.")] int? configuracionId = null,
        [Description("Parte de la descripción del nomenclador.")] string? nomenclador = null,
        [Description("Id de escala salarial.")] int? escalaSalarialId = null,
        [Description("Id de zona.")] int? zonaId = null,
        [Description("Período MM/yyyy o yyyy-MM para filtrar configuraciones vigentes en ese mes.")] string? vigenteEn = null,
        [Description("Usá 'Activa' si pide activas o vigentes sin una fecha, 'Futura' para futuras y 'Vencida' para vencidas. " +
                     "Para vigencia en un mes no agregues 'Activa' salvo que también lo solicite. Sin este parámetro se incluyen todos los estados.")] string? estado = null,
        [Description("Código o código/subcódigo del concepto asociado. Si indicó subcódigo, incluí ambos (ej. '25/100'). No es el Id interno ni la descripción.")] string? concepto = null,
        [Description("Id del tipo de valor fijo asociado, no el Id de un valor individual.")] int? valorFijoTipoId = null,
        [Description("Id del tipo de valor por categoría asociado, no el Id de un valor individual.")] int? valorCategoriaTipoId = null,
        [Description("Id interno del nomenclador si ya se conoce o el usuario eligió una opción.")] int? nomencladorId = null,
        [Description("Id interno del concepto si ya se conoce o el usuario eligió una opción.")] int? conceptoId = null)
    {
        var estadoDelModelo = estado;
        estado = AplicarEstadoSolicitado(estado);
        RegistrarEstadoAplicado(nameof(ObtenerConfiguracion), estadoDelModelo, estado);

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
                    coincidencias = respuesta,
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
        return (resultado, ResumirConfiguraciones(resultado, new
        {
            nomencladorId,
            escalaSalarialId,
            zonaId,
            vigenteEn = fecha?.ToString("MM/yyyy", CultureInfo.InvariantCulture),
            estado = string.IsNullOrWhiteSpace(estado) ? "Todos" : estado.Trim(),
            conceptoId,
            valorFijoTipoId,
            valorCategoriaTipoId,
        }));
    }

    private static object ResumirConfiguraciones(
        PagedResult<ConfiguracionNomencladorListItemDto> resultado, object filtrosAplicados) => new
    {
        resultado.Total,
        resultado.Page,
        resultado.PageSize,
        filtrosAplicados,
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

    private string? AplicarEstadoSolicitado(string? estadoDelModelo) =>
        estadoSolicitadoPorUsuario ?? estadoDelModelo;

    private void RegistrarEstadoAplicado(string herramienta, string? estadoDelModelo, string? estadoAplicado)
    {
        if (estadoSolicitadoPorUsuario is not null)
        {
            logger.LogInformation(
                "{Herramienta}: estado enviado por el modelo={EstadoModelo}; estado explícito del usuario aplicado={EstadoAplicado}.",
                herramienta, estadoDelModelo ?? "(omitido)", estadoAplicado);
        }
    }

    private static string? InferirEstadoSolicitado(string? consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
            return null;

        var consultaNormalizada = consulta.ToLowerInvariant();
        var solicitaVigenciaEnPeriodo = Regex.IsMatch(
            consultaNormalizada,
            @"\bvigentes?\s+(?:en|para)\b",
            RegexOptions.CultureInvariant);

        var estados = new List<string>(3);
        if (Regex.IsMatch(consultaNormalizada, @"\bactiv[oa]s?\b", RegexOptions.CultureInvariant))
            estados.Add("Activa");
        if (Regex.IsMatch(consultaNormalizada, @"\bfutur[oa]s?\b", RegexOptions.CultureInvariant))
            estados.Add("Futura");
        if (Regex.IsMatch(consultaNormalizada, @"\bvencid[oa]s?\b", RegexOptions.CultureInvariant))
            estados.Add("Vencida");
        if (!solicitaVigenciaEnPeriodo
            && Regex.IsMatch(consultaNormalizada, @"\bvigentes?\b", RegexOptions.CultureInvariant))
        {
            estados.Add("Activa");
        }

        return estados.Distinct(StringComparer.Ordinal).Take(2).Count() == 1
            ? estados[0]
            : null;
    }

    private static object Filtrar(IReadOnlyCollection<CatalogItemDto> items, string? texto)
    {
        var filtrados = items.Where(i => Contiene($"{i.Id}-{i.Descripcion}", texto)).ToList();
        return new
        {
            total = filtrados.Count,
            items = filtrados.Take(MaxResultados * 2).Select(i => new { i.Id, i.Descripcion }).ToList(),
        };
    }

    private static object FiltrarPorDescripcion(IReadOnlyCollection<CatalogItemDto> items, string descripcion)
    {
        var filtrados = items.Where(i => Contiene(i.Descripcion, descripcion)).ToList();
        return new
        {
            total = filtrados.Count,
            items = filtrados.Take(MaxResultados).Select(i => new { i.Id, i.Descripcion }).ToList(),
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

    private static string PrepararBusquedaConcepto(string? texto)
    {
        var busqueda = texto?.Trim() ?? string.Empty;
        if (busqueda.Length == 0)
            return string.Empty;

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
