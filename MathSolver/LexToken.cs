class LexToken
{
    public readonly string str;
    public readonly TokenType type;
    public LexToken(string str, TokenType type)
    {
        this.str = str;
        this.type = type;
    }
}

enum TokenType
{
    None, Identifier, Number, Operator, OpenParen, CloseParen, Subscript,
}