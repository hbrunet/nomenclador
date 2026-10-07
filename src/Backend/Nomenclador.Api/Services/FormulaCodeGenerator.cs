using System.Text;
using System.Text.RegularExpressions;

namespace Nomenclador.Api.Services;

/// <summary>
/// Reconstruye el PL/SQL generado por la app legacy a partir de Condicion/Accion y el catálogo
/// de Primitivas. El algoritmo fue reverso-ingenierizado comparando el código fuente real de
/// 2 fórmulas existentes (IDFORM 568 y 1360) hasta hacer match carácter a carácter — incluye
/// una particularidad del generador original: cuando una misma primitiva aparece más de una vez
/// dentro del MISMO texto (Condicion o Accion), cada aparición igual se numera y se declara con
/// su propio parámetro/llamada a PT_&lt;Primitiva&gt;, pero al sustituir el texto todas las
/// apariciones de esa primitiva se reemplazan por la variable de su PRIMERA aparición (no se
/// referencian las siguientes). Es un comportamiento "raro" pero se replica a propósito para no
/// apartarse de cómo ya vienen compiladas las fórmulas reales existentes.
/// </summary>
public static class FormulaCodeGenerator
{
    private static readonly Regex TokenRegex = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    private readonly record struct Token(string Name, int Index);

    public static string Generate(string spName, string condicion, string accion, IReadOnlyCollection<string> primitivaNames)
    {
        var knownNames = new HashSet<string>(primitivaNames, StringComparer.Ordinal);
        var nextIndex = 1;

        var condicionTokens = ExtractTokens(condicion, knownNames, ref nextIndex);
        var accionTokens = ExtractTokens(accion, knownNames, ref nextIndex);
        var todosLosTokens = condicionTokens.Concat(accionTokens).ToList();

        var condicionSustituida = Substitute(condicion, condicionTokens);
        var accionSustituida = Substitute(accion, accionTokens);

        var sb = new StringBuilder();
        sb.Append("CREATE OR REPLACE PROCEDURE ").Append(spName).Append("(\n");
        sb.Append("LH_IdPers IN NUMBER  DEFAULT NULL,\n");
        sb.Append("LH_IdOcupCarg IN NUMBER DEFAULT NULL, \n");
        sb.Append("LH_IdConcepto IN NUMBER  DEFAULT NULL,\n");
        foreach (var token in todosLosTokens)
            sb.Append("LH_").Append(token.Name).Append(token.Index).Append(" IN NUMBER  ,\n");
        sb.Append("salida IN OUT NUMBER) \n");
        sb.Append("AS \n");
        sb.Append("IdPers_ NUMBER(10,0) := LH_IdPers;\n");
        sb.Append("IdOcupCarg_ NUMBER(10,0) := LH_IdOcupCarg;\n");
        sb.Append("IdConcepto_ NUMBER(10,0) := LH_IdConcepto;\n");
        foreach (var token in todosLosTokens)
            sb.Append(token.Name).Append(token.Index).Append("_ NUMBER(12,6) := LH_").Append(token.Name).Append(token.Index).Append(";\n");
        sb.Append("BEGIN\n");
        foreach (var token in condicionTokens)
            sb.Append("PT_").Append(token.Name).Append("(IdPers_,  IdOcupCarg_ , IdConcepto_ ,").Append(token.Name).Append(token.Index).Append("_);\n");
        sb.Append("IF ").Append(condicionSustituida).Append("  THEN\n");
        sb.Append("BEGIN\n");
        foreach (var token in accionTokens)
            sb.Append("PT_").Append(token.Name).Append("(IdPers_,  IdOcupCarg_ , IdConcepto_ ,").Append(token.Name).Append(token.Index).Append("_);\n");
        sb.Append("select  ").Append(accionSustituida).Append("  into salida  FROM DUAL; \n");
        sb.Append("END; \n");
        sb.Append(" Else \n");
        sb.Append("BEGIN\n");
        sb.Append(" select -1 rta into salida  FROM DUAL; \n");
        sb.Append(" END; \n");
        sb.Append("END IF; \n");
        sb.Append("END ").Append(spName).Append(';');

        return sb.ToString();
    }

    private static List<Token> ExtractTokens(string? text, HashSet<string> knownNames, ref int nextIndex)
    {
        var tokens = new List<Token>();
        if (string.IsNullOrEmpty(text)) return tokens;

        foreach (Match match in TokenRegex.Matches(text))
        {
            if (!knownNames.Contains(match.Value)) continue;
            tokens.Add(new Token(match.Value, nextIndex));
            nextIndex++;
        }

        return tokens;
    }

    private static string Substitute(string? text, List<Token> tokens)
    {
        if (string.IsNullOrEmpty(text) || tokens.Count == 0) return text ?? string.Empty;

        var primerIndicePorNombre = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var token in tokens)
            primerIndicePorNombre.TryAdd(token.Name, token.Index);

        var sb = new StringBuilder();
        var lastEnd = 0;
        foreach (Match match in TokenRegex.Matches(text))
        {
            if (!primerIndicePorNombre.TryGetValue(match.Value, out var index)) continue;
            sb.Append(text, lastEnd, match.Index - lastEnd);
            sb.Append(match.Value).Append(index).Append('_');
            lastEnd = match.Index + match.Length;
        }
        sb.Append(text, lastEnd, text.Length - lastEnd);

        return sb.ToString();
    }

    // Prefijo FM_ + DescripcionBreve del concepto saneada a identificador Oracle válido + _<IDFORM>.
    public static string BuildSpName(string descripcionBreve, int idForm)
    {
        var sanitized = Regex.Replace(descripcionBreve.ToUpperInvariant(), "[^A-Z0-9]", "_").Trim('_');
        if (string.IsNullOrEmpty(sanitized)) sanitized = "FORMULA";

        var suffix = $"_{idForm}";
        var maxSanitizedLen = 30 - "FM_".Length - suffix.Length;
        if (sanitized.Length > maxSanitizedLen) sanitized = sanitized[..maxSanitizedLen];

        return $"FM_{sanitized}{suffix}";
    }
}
