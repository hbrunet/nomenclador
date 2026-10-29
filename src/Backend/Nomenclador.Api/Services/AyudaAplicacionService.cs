using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;

namespace Nomenclador.Api.Services;

public sealed class AyudaAplicacionService(IWebHostEnvironment environment)
{
    private const int MaxSecciones = 1;
    private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
    private static readonly HashSet<string> PalabrasIgnoradas =
    [
        "a", "al", "algo", "como", "con", "cual", "cuales", "cuando", "de", "del", "donde", "el", "en", "es",
        "esta", "este", "hay", "la", "las", "lo", "los", "mi", "me", "para", "por", "que", "se", "si", "sobre",
        "un", "una", "usar", "uso", "quiero", "puedo", "puede", "necesito", "ayuda", "aplicacion",
    ];

    private static readonly Dictionary<string, string> TerminosEquivalentes = new(StringComparer.Ordinal)
    {
        ["buscar"] = "buscar",
        ["busco"] = "buscar",
        ["busqueda"] = "buscar",
        ["consultar"] = "buscar",
        ["consulta"] = "buscar",
        ["encontrar"] = "buscar",
        ["filtrar"] = "buscar",
        ["asociar"] = "asociar",
        ["asocio"] = "asociar",
        ["asociacion"] = "asociar",
        ["asociaciones"] = "asociar",
        ["vincular"] = "asociar",
        ["vinculo"] = "asociar",
        ["desasociar"] = "asociar",
        ["desasociacion"] = "asociar",
        ["clonar"] = "clonar",
        ["clono"] = "clonar",
        ["clonacion"] = "clonar",
        ["copiar"] = "clonar",
        ["duplicar"] = "clonar",
        ["crear"] = "crear",
        ["creo"] = "crear",
        ["alta"] = "crear",
        ["nuevo"] = "crear",
        ["nueva"] = "crear",
        ["agregar"] = "crear",
        ["agrego"] = "crear",
        ["editar"] = "editar",
        ["edito"] = "editar",
        ["modificar"] = "editar",
        ["modifico"] = "editar",
        ["cambiar"] = "editar",
        ["cambio"] = "editar",
        ["actualizar"] = "actualizar",
        ["actualizacion"] = "actualizar",
        ["ajustar"] = "actualizar",
        ["eliminar"] = "eliminar",
        ["borro"] = "eliminar",
        ["borrar"] = "eliminar",
        ["quitar"] = "eliminar",
        ["quito"] = "eliminar",
        ["importe"] = "importe",
        ["monto"] = "importe",
    };

    public async Task<string> BuscarAsync(string consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
            return "Indicá qué pantalla o tarea de la aplicación querés consultar.";

        var terminosConsulta = Tokenizar(consulta)
            .Where(termino => !PalabrasIgnoradas.Contains(termino))
            .ToHashSet(StringComparer.Ordinal);
        if (terminosConsulta.Count == 0)
            return "Indicá el nombre de la pantalla o describí la tarea sobre la que necesitás ayuda.";

        var directorioManual = Path.Combine(environment.ContentRootPath, "docs", "ayuda");
        if (!Directory.Exists(directorioManual))
            throw new DirectoryNotFoundException($"No se encontró el directorio del manual de ayuda: {directorioManual}");

        var archivos = Directory.GetFiles(directorioManual, "*.md", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (archivos.Length == 0)
            throw new InvalidOperationException($"El directorio del manual de ayuda no contiene archivos Markdown: {directorioManual}");

        var secciones = new List<SeccionAyuda>();
        foreach (var archivo in archivos)
        {
            var markdown = await File.ReadAllTextAsync(archivo);
            secciones.AddRange(ExtraerSecciones(markdown, Path.GetFileNameWithoutExtension(archivo)));
        }

        if (secciones.Count == 0)
            throw new InvalidDataException("Los archivos del manual no contienen secciones Markdown con encabezados ##.");

        var resultados = secciones
            .Select(seccion => new
            {
                Seccion = seccion,
                TerminosTitulo = Tokenizar(seccion.Titulo).ToHashSet(StringComparer.Ordinal),
                TerminosContenido = Tokenizar(seccion.Contenido).ToHashSet(StringComparer.Ordinal),
            })
            .Select(item => new
            {
                item.Seccion,
                Puntaje = terminosConsulta.Sum(termino =>
                    (item.TerminosTitulo.Contains(termino) ? 3 : 0)
                    + (item.TerminosContenido.Contains(termino) ? 1 : 0)),
            })
            .Where(item => item.Puntaje > 0)
            .OrderByDescending(item => item.Puntaje)
            .ThenBy(item => item.Seccion.Titulo, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSecciones)
            .ToList();

        if (resultados.Count == 0)
            return "No encontré una sección del manual que responda claramente. ¿Podés decirme en qué pantalla estás o qué tarea querés realizar?";

        var respuesta = new StringBuilder("Secciones relevantes del manual de usuario:");
        foreach (var resultado in resultados)
        {
            respuesta.AppendLine();
            respuesta.AppendLine();
            respuesta.Append("### ").Append(resultado.Seccion.Documento).Append(" — ")
                .Append(resultado.Seccion.Titulo).AppendLine();
            respuesta.Append(resultado.Seccion.Contenido.Trim());
        }

        return respuesta.ToString();
    }

    private static IReadOnlyCollection<SeccionAyuda> ExtraerSecciones(string markdown, string nombreArchivo)
    {
        var secciones = new List<SeccionAyuda>();
        var tituloDocumento = nombreArchivo;
        string? titulo = null;
        var contenido = new StringBuilder();

        void GuardarSeccion()
        {
            var texto = contenido.ToString().Trim();
            if (titulo is not null && texto.Length > 0)
                secciones.Add(new SeccionAyuda(tituloDocumento, titulo, texto));
        }

        foreach (var linea in markdown.Split('\n'))
        {
            var textoLinea = linea.TrimEnd('\r');
            if (textoLinea.StartsWith("# ", StringComparison.Ordinal))
            {
                tituloDocumento = textoLinea[2..].Trim();
            }
            else if (textoLinea.StartsWith("## ", StringComparison.Ordinal))
            {
                GuardarSeccion();
                titulo = textoLinea[3..].Trim();
                contenido.Clear();
            }
            else if (titulo is not null)
            {
                contenido.AppendLine(textoLinea);
            }
        }

        GuardarSeccion();
        return secciones;
    }

    private static IEnumerable<string> Tokenizar(string texto)
    {
        foreach (Match match in TokenRegex.Matches(texto))
        {
            var termino = QuitarAcentos(match.Value);
            if (termino.Length > 5 && termino.EndsWith("ciones", StringComparison.Ordinal))
                termino = termino[..^2];
            else if (termino.Length > 4 && termino.EndsWith('s'))
                termino = termino[..^1];

            yield return TerminosEquivalentes.TryGetValue(termino, out var equivalente)
                ? equivalente
                : termino;
        }
    }

    private static string QuitarAcentos(string texto)
    {
        var normalizado = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(normalizado.Length);
        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                resultado.Append(caracter);
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record SeccionAyuda(string Documento, string Titulo, string Contenido);
}
