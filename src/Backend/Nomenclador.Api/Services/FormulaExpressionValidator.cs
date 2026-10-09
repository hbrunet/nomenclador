namespace Nomenclador.Api.Services;

/// <summary>
/// Valida que Condicion/Accion sean una expresión bien formada (no solo que los primitivas
/// existan): paréntesis balanceados en el orden correcto, operadores en la posición esperada,
/// ROUND(valor, decimales) con la forma correcta, etc. Grammar (recursive descent, LL(1)):
///
/// expr       := orExpr
/// orExpr     := andExpr (OR andExpr)*
/// andExpr    := notExpr (AND notExpr)*
/// notExpr    := NOT notExpr | comparacion
/// comparacion:= aditiva ((&lt; | &gt; | = | &lt;&gt; | &lt;= | &gt;=) aditiva)?
/// aditiva    := multiplicativa ((+|-) multiplicativa)*
/// multiplicativa := factor ((*|/) factor)*
/// factor     := (+|-) factor | NUMERO | IDENTIFICADOR | ROUND '(' aditiva ',' NUMERO ')' | '(' orExpr ')'
///
/// Esta misma gramática sirve tanto para Condicion (expresión booleana completa) como para
/// Accion (en la práctica solo usa el nivel aritmético, pero el nivel booleano es un superset
/// válido, así que no hace falta una gramática separada).
/// </summary>
public static class FormulaExpressionValidator
{
    private enum TokenType { Number, Identifier, Plus, Minus, Star, Slash, Lt, Gt, Eq, Ne, Le, Ge, LParen, RParen, Comma, Eof }

    private readonly record struct Token(TokenType Type, string Text);

    private sealed class FormulaSyntaxException(string message) : Exception(message);

    // Devuelve null si la expresión está bien formada, o un mensaje describiendo el primer error.
    public static string? Validar(string? expresion)
    {
        if (string.IsNullOrWhiteSpace(expresion)) return null;

        try
        {
            var parser = new Parser(Tokenizar(expresion));
            parser.ParseExpresion();
            parser.ExpectEof();
            return null;
        }
        catch (FormulaSyntaxException ex)
        {
            return ex.Message;
        }
    }

    private static List<Token> Tokenizar(string texto)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < texto.Length)
        {
            var c = texto[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c))
            {
                var start = i;
                while (i < texto.Length && (char.IsDigit(texto[i]) || texto[i] == '.')) i++;
                tokens.Add(new Token(TokenType.Number, texto[start..i]));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < texto.Length && (char.IsLetterOrDigit(texto[i]) || texto[i] == '_')) i++;
                tokens.Add(new Token(TokenType.Identifier, texto[start..i]));
                continue;
            }

            switch (c)
            {
                case '+': tokens.Add(new Token(TokenType.Plus, "+")); i++; break;
                case '-': tokens.Add(new Token(TokenType.Minus, "-")); i++; break;
                case '*': tokens.Add(new Token(TokenType.Star, "*")); i++; break;
                case '/': tokens.Add(new Token(TokenType.Slash, "/")); i++; break;
                case '(': tokens.Add(new Token(TokenType.LParen, "(")); i++; break;
                case ')': tokens.Add(new Token(TokenType.RParen, ")")); i++; break;
                case ',': tokens.Add(new Token(TokenType.Comma, ",")); i++; break;
                case '<':
                    if (i + 1 < texto.Length && texto[i + 1] == '=') { tokens.Add(new Token(TokenType.Le, "<=")); i += 2; }
                    else if (i + 1 < texto.Length && texto[i + 1] == '>') { tokens.Add(new Token(TokenType.Ne, "<>")); i += 2; }
                    else { tokens.Add(new Token(TokenType.Lt, "<")); i++; }
                    break;
                case '>':
                    if (i + 1 < texto.Length && texto[i + 1] == '=') { tokens.Add(new Token(TokenType.Ge, ">=")); i += 2; }
                    else { tokens.Add(new Token(TokenType.Gt, ">")); i++; }
                    break;
                case '=': tokens.Add(new Token(TokenType.Eq, "=")); i++; break;
                default:
                    throw new FormulaSyntaxException($"carácter inesperado '{c}'.");
            }
        }
        tokens.Add(new Token(TokenType.Eof, string.Empty));
        return tokens;
    }

    private sealed class Parser(List<Token> tokens)
    {
        private int pos;
        private Token Current => tokens[pos];

        private bool IsKeyword(string palabra) =>
            Current.Type == TokenType.Identifier && string.Equals(Current.Text, palabra, StringComparison.OrdinalIgnoreCase);

        private void Advance() => pos++;

        public void ExpectEof()
        {
            if (Current.Type != TokenType.Eof)
                throw new FormulaSyntaxException($"sobra texto luego de la expresión, cerca de '{Current.Text}'.");
        }

        public void ParseExpresion() => ParseOr();

        private void ParseOr()
        {
            ParseAnd();
            while (IsKeyword("OR")) { Advance(); ParseAnd(); }
        }

        private void ParseAnd()
        {
            ParseNot();
            while (IsKeyword("AND")) { Advance(); ParseNot(); }
        }

        private void ParseNot()
        {
            if (IsKeyword("NOT")) { Advance(); ParseNot(); return; }
            ParseComparacion();
        }

        private void ParseComparacion()
        {
            ParseAditiva();
            if (Current.Type is TokenType.Lt or TokenType.Gt or TokenType.Eq or TokenType.Ne or TokenType.Le or TokenType.Ge)
            {
                Advance();
                ParseAditiva();
            }
        }

        private void ParseAditiva()
        {
            ParseMultiplicativa();
            while (Current.Type is TokenType.Plus or TokenType.Minus)
            {
                Advance();
                ParseMultiplicativa();
            }
        }

        private void ParseMultiplicativa()
        {
            ParseFactor();
            while (Current.Type is TokenType.Star or TokenType.Slash)
            {
                Advance();
                ParseFactor();
            }
        }

        private void ParseFactor()
        {
            if (Current.Type is TokenType.Plus or TokenType.Minus)
            {
                Advance();
                ParseFactor();
                return;
            }

            if (Current.Type == TokenType.Number) { Advance(); return; }

            if (Current.Type == TokenType.LParen)
            {
                Advance();
                ParseOr();
                if (Current.Type != TokenType.RParen)
                    throw new FormulaSyntaxException("falta un paréntesis de cierre ')'.");
                Advance();
                return;
            }

            if (IsKeyword("ROUND"))
            {
                Advance();
                if (Current.Type != TokenType.LParen)
                    throw new FormulaSyntaxException("se esperaba '(' después de ROUND.");
                Advance();
                ParseAditiva();
                if (Current.Type != TokenType.Comma)
                    throw new FormulaSyntaxException("se esperaba ',' dentro de ROUND(valor, decimales).");
                Advance();
                if (Current.Type != TokenType.Number)
                    throw new FormulaSyntaxException("ROUND espera un número de decimales como segundo argumento.");
                Advance();
                if (Current.Type != TokenType.RParen)
                    throw new FormulaSyntaxException("falta el paréntesis de cierre de ROUND(...).");
                Advance();
                return;
            }

            if (Current.Type == TokenType.Identifier)
            {
                if (IsKeyword("AND") || IsKeyword("OR") || IsKeyword("NOT"))
                    throw new FormulaSyntaxException($"no se esperaba '{Current.Text}' en esa posición.");
                Advance();
                return;
            }

            if (Current.Type == TokenType.Eof)
                throw new FormulaSyntaxException("la expresión terminó de forma inesperada.");

            throw new FormulaSyntaxException($"no se esperaba '{Current.Text}' en esa posición.");
        }
    }
}
