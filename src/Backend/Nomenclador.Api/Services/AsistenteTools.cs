using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.AI;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Repositories;

namespace Nomenclador.Api.Services;

// Funciones expuestas al modelo (function calling). Las de lectura consultan los servicios/repositorios
// existentes; las de escritura sólo registran una propuesta que el usuario debe confirmar en el frontend.
public sealed class AsistenteTools(
    ConfiguracionNomencladorService configuracionService,
    ClonadoConfiguracionService clonadoService,
    CatalogRepository catalogRepository,
    ConceptoRepository conceptoRepository)
{
    private const int MaxResultados = 25;

    private readonly List<AsistenteAccionPropuestaDto> acciones = [];

    public IReadOnlyList<AsistenteAccionPropuestaDto> AccionesPropuestas => acciones;

    public IList<AITool> CrearTools() =>
        typeof(AsistenteTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<DescriptionAttribute>() is not null)
            .Select(method => (AITool)AIFunctionFactory.Create(method, this))
            .ToList();

    // ---------- Lectura ----------

    [Description("Devuelve la fecha de hoy y el período activo de liquidación (primer día del mes, formato yyyy-MM-dd).")]
    public async Task<object> ObtenerPeriodoActivo()
    {
        var periodo = await catalogRepository.GetPeriodoActivoAsync();
        return new { hoy = Formatear(DateOnly.FromDateTime(DateTime.Today)), periodoActivo = Formatear(periodo) };
    }

    [Description("Lista nomencladores (catálogo). Filtra opcionalmente por texto contenido en la descripción.")]
    public async Task<object> BuscarNomencladores([Description("Texto a buscar (opcional).")] string? texto = null)
        => Filtrar(await catalogRepository.GetNomencladoresAsync(), texto);

    [Description("Lista escalas salariales (catálogo). Filtra opcionalmente por texto contenido en la descripción.")]
    public async Task<object> BuscarEscalasSalariales([Description("Texto a buscar (opcional).")] string? texto = null)
        => Filtrar(await catalogRepository.GetEscalasAsync(), texto);

    [Description("Lista zonas (catálogo). Filtra opcionalmente por texto contenido en la descripción.")]
    public async Task<object> BuscarZonas([Description("Texto a buscar (opcional).")] string? texto = null)
        => Filtrar(await catalogRepository.GetZonasAsync(), texto);

    [Description("Lista las categorías de una escala salarial con su número y monto.")]
    public async Task<object> ListarCategoriasDeEscala([Description("Id de la escala salarial.")] int escalaSalarialId)
    {
        var categorias = await catalogRepository.GetCategoriasAsync(escalaSalarialId);
        return categorias.Select(c => new { c.Id, c.Numero, c.Descripcion, c.Monto }).ToList();
    }

    [Description("Busca conceptos de liquidación por código o descripción. Devuelve hasta 25 resultados.")]
    public async Task<object> BuscarConceptos([Description("Texto o código a buscar.")] string texto)
    {
        var conceptos = await conceptoRepository.GetAllAsync(texto);
        return new
        {
            total = conceptos.Count,
            items = conceptos.Take(MaxResultados).Select(c => new
            {
                c.Id, c.Codigo, c.Subcodigo, c.Descripcion, c.DescripcionBreve, c.TipoConcepto,
            }).ToList(),
        };
    }

    [Description("Obtiene el detalle completo de un concepto (flags de acumulación, meses aplicables, tipos de liquidación, fórmulas).")]
    public async Task<object> ObtenerConcepto([Description("Id del concepto.")] int conceptoId)
        => (object?)await conceptoRepository.GetByIdAsync(conceptoId) ?? $"No existe el concepto {conceptoId}.";

    [Description("Busca valores fijos por descripción o tipo. Devuelve hasta 25 resultados con su valor.")]
    public async Task<object> BuscarValoresFijos([Description("Texto a buscar (opcional).")] string? texto = null)
    {
        var valores = (await catalogRepository.GetValoresFijosAsync())
            .Where(v => Contiene(v.Descripcion, texto) || Contiene(v.Tipo, texto))
            .ToList();
        return new
        {
            total = valores.Count,
            items = valores.Take(MaxResultados).Select(v => new { v.Id, v.Descripcion, v.Tipo, v.Valor }).ToList(),
        };
    }

    [Description("Indica en cuántas configuraciones se usa un valor fijo.")]
    public async Task<object> ContarUsosValorFijo([Description("Id del valor fijo.")] int valorFijoId)
        => await catalogRepository.GetValorFijoUsagesAsync(valorFijoId);

    [Description("Busca valores por categoría por descripción o tipo. Devuelve hasta 25 resultados.")]
    public async Task<object> BuscarValoresPorCategoria([Description("Texto a buscar (opcional).")] string? texto = null)
    {
        var valores = (await catalogRepository.GetValoresCategoriasAsync())
            .Where(v => Contiene(v.Descripcion, texto) || Contiene(v.Tipo, texto))
            .ToList();
        return new
        {
            total = valores.Count,
            items = valores.Take(MaxResultados).Select(v => new { v.Id, v.Descripcion, v.Tipo }).ToList(),
        };
    }

    [Description("Busca configuraciones de nomenclador con filtros opcionales. Todos los filtros son ids salvo fechas y estado. " +
                 "Devuelve como máximo 50 resultados por página junto con el total.")]
    public async Task<object> BuscarConfiguraciones(
        [Description("Id de nomenclador.")] int? nomencladorId = null,
        [Description("Id de escala salarial.")] int? escalaSalarialId = null,
        [Description("Id de zona.")] int? zonaId = null,
        [Description("Fecha yyyy-MM-dd: sólo configuraciones vigentes en esa fecha.")] string? vigenteEn = null,
        [Description("Estado: Futura, Activa o Vencida.")] string? estado = null,
        [Description("Id de concepto que debe contener la configuración.")] int? conceptoId = null,
        [Description("Id de valor fijo que debe contener la configuración.")] int? valorFijoId = null,
        [Description("Id de valor por categoría que debe contener la configuración.")] int? valorCategoriaId = null,
        [Description("Número de página (desde 1).")] int pagina = 1)
    {
        var fecha = ParsearFecha(vigenteEn);
        var result = await configuracionService.GetAllAsync(
            nomencladorId, escalaSalarialId, zonaId, fecha, estado, Math.Max(1, pagina), 50,
            conceptoId, valorFijoId, valorCategoriaId);

        return new
        {
            result.Total,
            result.Page,
            result.PageSize,
            items = result.Items.Select(i => new
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
    }

    [Description("Obtiene el detalle de una configuración: conceptos, valores fijos y valores por categoría asociados.")]
    public async Task<object> ObtenerConfiguracion([Description("Id de la configuración.")] int configuracionId)
    {
        try
        {
            var d = await configuracionService.GetByIdAsync(configuracionId);
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
                ValoresFijos = d.ValoresFijos.Select(v => new { v.IdValorFijo, v.Descripcion, v.Tipo, v.Valor }).ToList(),
                ValoresCategorias = d.ValoresCategorias.Select(v => new
                {
                    v.IdValorCategoria, v.Descripcion, v.Tipo, CantidadItems = v.Items.Count,
                }).ToList(),
            };
        }
        catch (KeyNotFoundException ex)
        {
            return ex.Message;
        }
    }

    // ---------- Escritura (sólo propuestas, requieren confirmación del usuario) ----------

    [Description("PROPONE clonar una configuración a un nuevo rango de fechas. No ejecuta nada: el usuario debe confirmar. " +
                 "Valida previamente superposición de vigencias y devuelve los errores si los hay.")]
    public async Task<object> ProponerClonarConfiguracion(
        [Description("Id de la configuración origen.")] int configuracionId,
        [Description("Fecha de inicio yyyy-MM-dd (se normaliza al primer día del mes).")] string fechaInicio,
        [Description("Fecha de fin yyyy-MM-dd (opcional, sin fin si se omite).")] string? fechaFin = null,
        [Description("Copiar conceptos.")] bool copiarConceptos = true,
        [Description("Copiar valores fijos.")] bool copiarValoresFijos = true,
        [Description("Copiar valores por categoría.")] bool copiarValoresCategoria = true)
    {
        var inicio = ParsearFecha(fechaInicio);
        if (inicio is null) return "La fecha de inicio es obligatoria y debe tener formato yyyy-MM-dd.";

        ConfiguracionNomencladorDetailDto origen;
        try
        {
            origen = await configuracionService.GetByIdAsync(configuracionId);
        }
        catch (KeyNotFoundException ex)
        {
            return ex.Message;
        }

        var request = new ClonarConfiguracionDto
        {
            FechaInicio = inicio.Value,
            FechaFin = ParsearFecha(fechaFin),
            CopiarConceptos = copiarConceptos,
            CopiarValoresFijos = copiarValoresFijos,
            CopiarValoresCategoria = copiarValoresCategoria,
        };

        var validacion = await configuracionService.ValidateAsync(clonadoService.BuildClone(origen, request));
        if (!validacion.Valida)
        {
            return new { propuestaCreada = false, errores = validacion.Errores.Select(e => e.Mensaje).ToList() };
        }

        return Registrar(new AsistenteAccionPropuestaDto
        {
            Tipo = "clonar-configuracion",
            ConfiguracionId = configuracionId,
            Clonar = request,
            Descripcion = $"Clonar la configuración {configuracionId} ({origen.NomencladorDescripcion} / {origen.EscalaDescripcion} / " +
                          $"{origen.ZonaDescripcion}) desde {Formatear(request.FechaInicio)}" +
                          (request.FechaFin is null ? " sin fecha de fin" : $" hasta {Formatear(request.FechaFin.Value)}") + ".",
        }, validacion.Warnings.Select(w => w.Mensaje));
    }

    [Description("PROPONE agregar un concepto a una configuración. No ejecuta nada: el usuario debe confirmar.")]
    public async Task<object> ProponerAgregarConcepto(
        [Description("Id de la configuración.")] int configuracionId,
        [Description("Id del concepto.")] int conceptoId)
    {
        var (config, error) = await ObtenerDetalleAsync(configuracionId);
        if (config is null) return error!;

        var concepto = await conceptoRepository.GetByIdAsync(conceptoId);
        if (concepto is null) return $"No existe el concepto {conceptoId}.";
        if (config.Conceptos.Any(c => c.IdConcepto == conceptoId))
            return "La configuración ya contiene ese concepto.";

        return Registrar(new AsistenteAccionPropuestaDto
        {
            Tipo = "agregar-concepto",
            ConfiguracionId = configuracionId,
            ConceptoId = conceptoId,
            Descripcion = $"Agregar el concepto {concepto.Codigo}/{concepto.Subcodigo} - {concepto.Descripcion} a la configuración {configuracionId}.",
        });
    }

    [Description("PROPONE quitar un concepto de una configuración. No ejecuta nada: el usuario debe confirmar.")]
    public async Task<object> ProponerQuitarConcepto(
        [Description("Id de la configuración.")] int configuracionId,
        [Description("Id del concepto.")] int conceptoId)
    {
        var (config, error) = await ObtenerDetalleAsync(configuracionId);
        if (config is null) return error!;

        var concepto = config.Conceptos.FirstOrDefault(c => c.IdConcepto == conceptoId);
        if (concepto is null) return "La configuración no contiene ese concepto.";

        return Registrar(new AsistenteAccionPropuestaDto
        {
            Tipo = "quitar-concepto",
            ConfiguracionId = configuracionId,
            ConceptoId = conceptoId,
            Descripcion = $"Quitar el concepto {concepto.Codigo}/{concepto.Subcodigo} - {concepto.Descripcion} de la configuración {configuracionId}.",
        });
    }

    [Description("PROPONE asociar un valor fijo a una configuración (reemplaza al valor fijo del mismo tipo si existe). " +
                 "No ejecuta nada: el usuario debe confirmar.")]
    public async Task<object> ProponerAgregarValorFijo(
        [Description("Id de la configuración.")] int configuracionId,
        [Description("Id del valor fijo.")] int valorFijoId)
    {
        var (config, error) = await ObtenerDetalleAsync(configuracionId);
        if (config is null) return error!;

        var valor = (await catalogRepository.GetValoresFijosAsync()).FirstOrDefault(v => v.Id == valorFijoId);
        if (valor is null) return $"No existe el valor fijo {valorFijoId}.";
        if (config.ValoresFijos.Any(v => v.IdValorFijo == valorFijoId))
            return "La configuración ya contiene ese valor fijo.";

        var reemplazado = config.ValoresFijos.FirstOrDefault(v => v.IdTipo == valor.IdTipo);
        return Registrar(new AsistenteAccionPropuestaDto
        {
            Tipo = "agregar-valor-fijo",
            ConfiguracionId = configuracionId,
            ValorFijoId = valorFijoId,
            Descripcion = $"Asociar el valor fijo \"{valor.Descripcion}\" ({valor.Tipo}, {valor.Valor.ToString(CultureInfo.InvariantCulture)}) " +
                          $"a la configuración {configuracionId}" +
                          (reemplazado is null ? "." : $", reemplazando a \"{reemplazado.Descripcion}\"."),
        });
    }

    [Description("PROPONE quitar un valor fijo de una configuración. No ejecuta nada: el usuario debe confirmar.")]
    public async Task<object> ProponerQuitarValorFijo(
        [Description("Id de la configuración.")] int configuracionId,
        [Description("Id del valor fijo.")] int valorFijoId)
    {
        var (config, error) = await ObtenerDetalleAsync(configuracionId);
        if (config is null) return error!;

        var valor = config.ValoresFijos.FirstOrDefault(v => v.IdValorFijo == valorFijoId);
        if (valor is null) return "La configuración no contiene ese valor fijo.";

        return Registrar(new AsistenteAccionPropuestaDto
        {
            Tipo = "quitar-valor-fijo",
            ConfiguracionId = configuracionId,
            ValorFijoId = valorFijoId,
            Descripcion = $"Quitar el valor fijo \"{valor.Descripcion}\" ({valor.Tipo}) de la configuración {configuracionId}.",
        });
    }

    // ---------- Helpers ----------

    private object Registrar(AsistenteAccionPropuestaDto accion, IEnumerable<string>? advertencias = null)
    {
        acciones.Add(accion);
        return new
        {
            propuestaCreada = true,
            mensaje = "Propuesta registrada. Se mostrará al usuario un botón para confirmarla; todavía NO se ejecutó.",
            accion.Descripcion,
            advertencias = advertencias?.ToList() ?? [],
        };
    }

    private async Task<(ConfiguracionNomencladorDetailDto? Config, string? Error)> ObtenerDetalleAsync(int id)
    {
        try
        {
            return (await configuracionService.GetByIdAsync(id), null);
        }
        catch (KeyNotFoundException ex)
        {
            return (null, ex.Message);
        }
    }

    private static object Filtrar(IReadOnlyCollection<CatalogItemDto> items, string? texto)
    {
        var filtrados = items.Where(i => Contiene(i.Descripcion, texto)).ToList();
        return new
        {
            total = filtrados.Count,
            items = filtrados.Take(MaxResultados * 2).Select(i => new { i.Id, i.Descripcion }).ToList(),
        };
    }

    private static bool Contiene(string? valor, string? texto) =>
        string.IsNullOrWhiteSpace(texto)
        || (valor?.Contains(texto.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

    private static DateOnly? ParsearFecha(string? valor) =>
        DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : null;

    private static string Formatear(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
