using System.Text;

static class Lexer
{
    private static bool IsAlpha(char ch)
    {
        if (ch >= 'A' && ch <= 'Z')
        {
            return true;
        }
        else if (ch >= 'a' && ch <= 'z')
        {
            return true;
        }
        return false;
    }
    private static bool IsDigit(char ch)
    {
        if (ch >= '0' && ch <= '9')
        {
            return true;
        }
        return false;
    }

    private static LexToken ConsumeIdentifier(char ch, TextReader reader, StringBuilder sb)
    {
        sb.Append(ch);
        ch = (char)reader.Peek();
        while (IsAlpha(ch))
        {
            sb.Append(ch);
            ch = (char)reader.Peek();
        }
        string str = sb.ToString();
        sb.Clear();
        return new LexToken(str, TokenType.Identifier);
    }
    private static LexToken ConsumeNumber(char ch, TextReader reader, StringBuilder sb)
    {
        sb.Append(ch);
        ch = (char)reader.Peek();
        while (IsDigit(ch))
        {
            sb.Append(ch);
            ch = (char)reader.Peek();
        }
        string str = sb.ToString();
        sb.Clear();
        return new LexToken(str, TokenType.Number);
    }
    public static List<LexToken> Tokenize(TextReader reader)
    {
        string[] operators = ["+", "-", "*", "/", "^"];
        char[] whitespace = [' ', '\r', '\n', '\t'];
        List<LexToken> tokens = new List<LexToken>();
        StringBuilder sb = new StringBuilder();
        while (reader.Peek() > 0)
        {
            char ch = (char)reader.Read();
            if (IsAlpha(ch))
            {
                tokens.Add(ConsumeIdentifier(ch, reader, sb));
            }
            else if (IsDigit(ch))
            {
                tokens.Add(ConsumeNumber(ch, reader, sb));
            }
            else if (ch == '_')
            {
                tokens.Add(new LexToken(ch.ToString(), TokenType.Subscript));
            }
            else if (ch == '.')
            {
                tokens.Add(new LexToken(ch.ToString(), TokenType.Dot));
            }
            else if (ch == '(')
            {
                tokens.Add(new LexToken(ch.ToString(), TokenType.OpenParen));
            }
            else if (ch == ')')
            {
                tokens.Add(new LexToken(ch.ToString(), TokenType.CloseParen));
            }
            else if (whitespace.Contains(ch))
            {
                //skip whitespace
            }
            else if (operators.Contains(ch.ToString()))
            {
                tokens.Add(new LexToken(ch.ToString(), TokenType.Operator));
            }
            else
            {
                //TODO proper error handling
                throw new Exception("Lexer error");
            }
        }
        return tokens;
    }
}