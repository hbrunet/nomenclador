namespace Nomenclador.Api.Services;

/// <summary>
/// Valida que Condicion/Accion sean una expresión bien formada (no solo que las primitivas
/// existan): paréntesis balanceados en el orden correcto, operadores en la posición esperada,
/// ROUND(valor, decimales) con la forma correcta, etc. Gramática (descenso recursivo):
///
/// condicion := orCondicion
/// orCondicion := andCondicion (OR andCondicion)*
/// andCondicion := notCondicion (AND notCondicion)*
/// notCondicion := NOT notCondicion | predicado
/// predicado := aritmetica comparador aritmetica | '(' condicion ')'
/// accion := aritmetica
/// aritmetica := multiplicativa ((+|-) multiplicativa)*
/// multiplicativa := factor ((*|/) factor)*
/// factor := (+|-) factor | NUMERO | IDENTIFICADOR | ROUND '(' aritmetica ',' NUMERO ')' | '(' aritmetica ')'
///
/// Las condiciones requieren predicados y las acciones solo aceptan expresiones aritméticas.
/// </summary>
public static class FormulaExpressionValidator
{
    private enum TokenType { Number, Identifier, Plus, Minus, Star, Slash, Lt, Gt, Eq, Ne, Le, Ge, LParen, RParen, Comma, Eof }

    private readonly record struct Token(TokenType Type, string Text);

    private sealed class FormulaSyntaxException(string message) : Exception(message);

    // Devuelve null si la expresión está bien formada, o un mensaje describiendo el primer error.
    public static string? ValidarCondicion(string? expresion) =>
        Validar(expresion, parser => parser.ParseCondicion());

    public static string? ValidarAccion(string? expresion) =>
        Validar(expresion, parser => parser.ParseAccion());

    private static string? Validar(string? expresion, Action<Parser> parse)
    {
        if (string.IsNullOrWhiteSpace(expresion)) return null;

        try
        {
            var parser = new Parser(Tokenizar(expresion));
            parse(parser);
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
                while (i < texto.Length && char.IsDigit(texto[i])) i++;
                if (i < texto.Length && texto[i] == '.')
                {
                    i++;
                    while (i < texto.Length && char.IsDigit(texto[i])) i++;
                }
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

        public void ParseCondicion() => ParseOrCondicion();

        public void ParseAccion() => ParseAditiva();

        private void ParseOrCondicion()
        {
            ParseAndCondicion();
            while (IsKeyword("OR")) { Advance(); ParseAndCondicion(); }
        }

        private void ParseAndCondicion()
        {
            ParseNotCondicion();
            while (IsKeyword("AND")) { Advance(); ParseNotCondicion(); }
        }

        private void ParseNotCondicion()
        {
            if (IsKeyword("NOT")) { Advance(); ParseNotCondicion(); return; }
            ParsePredicado();
        }

        private void ParsePredicado()
        {
            var inicio = pos;
            try
            {
                ParseAditiva();
                if (Current.Type is not (TokenType.Lt or TokenType.Gt or TokenType.Eq or TokenType.Ne or TokenType.Le or TokenType.Ge))
                    throw new FormulaSyntaxException("se esperaba una comparación en la condición.");
                Advance();
                ParseAditiva();
                return;
            }
            catch (FormulaSyntaxException)
            {
                pos = inicio;
                if (Current.Type != TokenType.LParen) throw;

                Advance();
                ParseOrCondicion();
                if (Current.Type != TokenType.RParen)
                    throw new FormulaSyntaxException("falta un paréntesis de cierre ')'.");
                Advance();
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
                ParseAditiva();
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
