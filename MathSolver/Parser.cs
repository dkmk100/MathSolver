using System.Numerics;
using System.Security.Cryptography.X509Certificates;

static class Parser
{
    private class ParserState
    {
        private readonly List<LexToken> tokens;
        private int id = 0;
        public ParserState(List<LexToken> tokens)
        {
            this.tokens = tokens;
        }
        public LexToken Peek()
        {
            if (id >= tokens.Count)
            {
                return new LexToken("", TokenType.None);
            }
            return tokens[id];
        }
        public LexToken Pop()
        {
            id += 1;
            return tokens[id - 1];
        }
    }
    public static RootNode Parse(List<LexToken> tokens)
    {
        ParserState state = new ParserState(tokens);
        return ParseRoot(state);
    }
    private static RootNode ParseRoot(ParserState state)
    {
        LexToken token = state.Pop();
        if (token.type != TokenType.Identifier)
        {
            throw new Exception("Parse error: expected identifier");
        }
        if (token.str == "Solve")
        {
            if (state.Pop().type != TokenType.OpenBracket)
            {
                throw new Exception("Parse error: expected '{'");
            }

            ExpNode left = ParseExpression(state);
            if (state.Pop().type != TokenType.Equals)
            {
                throw new Exception("Parse error: expected '='");
            }
            ExpNode right = ParseExpression(state);

            if (state.Pop().type != TokenType.Comma)
            {
                throw new Exception("Parse error: expected ','");
            }
            var target = state.Pop();
            if (target.type != TokenType.Identifier)
            {
                throw new Exception("Parse error: expected identifier");
            }

            if (state.Pop().type != TokenType.CloseBracket)
            {
                throw new Exception("Parse error: expected '}'");
            }

            return new RootNode_Solve(left, right, target.str);
        }
        else if (token.str == "Simplify")
        {
            if (state.Pop().type != TokenType.OpenBracket)
            {
                throw new Exception("Parse error: expected '{'");
            }

            ExpNode inner = ParseExpression(state);

            if (state.Pop().type != TokenType.CloseBracket)
            {
                throw new Exception("Parse error: expected '}'");
            }
            return new RootNode_Simplify(inner);
        }
        else
        {
            throw new Exception("Parse error: unknown instruction");
        }
    }
    private static ExpNode ParseExpression(ParserState state)
    {
        return ParseExpPlus(state);
    }

    private static ExpNode ParseExpPlus(ParserState state)
    {
        ExpNode left = ParseExpTimes(state);
        string[] ops = ["+", "-"];
        if (state.Peek().type == TokenType.Operator && ops.Contains(state.Peek().str))
        {
            List<ExpNode> inner = [left];
            while (state.Peek().type == TokenType.Operator && ops.Contains(state.Peek().str))
            {
                LexToken token = state.Peek();
                if (token.str == "+")
                {
                    state.Pop();
                    left = ParseExpTimes(state);
                    inner.Add(left);
                }
                else if (token.str == "-")
                {
                    state.Pop();
                    left = ParseExpTimes(state);
                    inner.Add(new ExpNode_Negate(left));
                }
                else
                {
                    throw new Exception("Internal parse error");
                }
            }
            return new ExpNode_Plus(inner.ToArray());
        }
        else
        {
            return left;
        }
    }

    private static ExpNode ParseExpTimes(ParserState state)
    {
        ExpNode left = ParseExpPow(state);
        string[] ops = ["*", "/"];
        if (state.Peek().type == TokenType.Operator && ops.Contains(state.Peek().str))
        {
            List<ExpNode> inner = [left];
            while (state.Peek().type == TokenType.Operator && ops.Contains(state.Peek().str))
            {
                LexToken token = state.Peek();
                if (token.str == "*")
                {
                    state.Pop();
                    left = ParseExpPow(state);
                    inner.Add(left);
                }
                else if (token.str == "/")
                {
                    state.Pop();
                    left = ParseExpPow(state);
                    inner.Add(new ExpNode_Invert(left));
                }
                else
                {
                    throw new Exception("Internal parse error");
                }
            }
            return new ExpNode_Times(inner.ToArray());
        }
        else
        {
            return left;
        }
    }
    private static ExpNode ParseExpPow(ParserState state)
    {
        ExpNode left = ParseExpNeg(state);
        if (state.Peek().type == TokenType.Operator && state.Peek().str == "^")
        {
            state.Pop();
            ExpNode right = ParseExpNeg(state);
            return new ExpNode_Pow(left, right);
        }
        else
        {
            return left;
        }
    }
    private static ExpNode ParseExpNeg(ParserState state)
    {
        if (state.Peek().type == TokenType.Operator && state.Peek().str == "-")
        {
            state.Pop();
            ExpNode inner = ParseExpLeaf(state);
            return new ExpNode_Negate(inner);
        }
        else
        {
            return ParseExpLeaf(state);
        }
    }
    private static ExpNode ParseExpLeaf(ParserState state)
    {
        LexToken next = state.Peek();
        if (next.type == TokenType.OpenParen)
        {
            state.Pop();
            ExpNode node = ParseExpression(state);
            if (state.Peek().type != TokenType.CloseParen)
            {
                throw new Exception("Parse error: expected ')'");
            }
            state.Pop();
            return node;
        }
        else if (next.type == TokenType.Number)
        {
            state.Pop();
            BigInteger val = BigInteger.Parse(next.str);
            if (state.Peek().type == TokenType.Dot)
            {
                state.Pop();
                LexToken other = state.Pop();
                if (other.type != TokenType.Number)
                {
                    throw new Exception("Parse error: expected number");
                }
                BigInteger dec = BigInteger.Parse(other.str);
                BigInteger denominator = BigInteger.Pow(10, other.str.Length);
                BigFraction frac = new BigFraction(val * denominator + dec, denominator);
                return new ExpNode_Num(frac);
            }
            else
            {
                BigFraction frac = new BigFraction(val, 1);
                return new ExpNode_Num(frac);
            }
        }
        else if (next.type == TokenType.Identifier)
        {
            return ParseVar(state);
        }
        throw new Exception("Parse error: invalid expression");
    }

    private static ExpNode ParseVar(ParserState state)
    {
        LexToken next = state.Pop();
        if (next.type != TokenType.Identifier)
        {
            throw new Exception("Parse error: expected identifier");
        }
        ExpNode? subscript = null;
        if (state.Peek().type == TokenType.Subscript)
        {
            state.Pop();
            subscript = ParseExpLeaf(state);
        }
        return new ExpNode_Var(next.str, subscript);
    }
}