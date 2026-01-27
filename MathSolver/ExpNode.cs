using System.Diagnostics.Tracing;

interface ExpNode
{
    public string PrettyPrint();
    public int Precedence();
}

enum BOperator
{
    None, Plus, Minus, Times, Div, Pow
}
static class BOperatorExtensions
{
    public static string PrettyPrint(this BOperator self)
    {
        return self switch
        {
            BOperator.None => "ERROR",
            BOperator.Pow => "^",
            BOperator.Times => "*",
            BOperator.Div => "/",
            BOperator.Plus => "+",
            BOperator.Minus => "-",
            _ => "ERROR",
        };
    }
    public static int Precedence(this BOperator self)
    {
        return self switch
        {
            BOperator.None => int.MaxValue,
            BOperator.Pow => 100,
            BOperator.Times => 200,
            BOperator.Div => 200,
            BOperator.Plus => 300,
            BOperator.Minus => 300,
            _ => int.MaxValue,
        };
    }
}

sealed record class ExpNode_BOp(BOperator op, ExpNode left, ExpNode right) : ExpNode
{
    public string PrettyPrint()
    {
        //TODO handle associativity
        int p = op.Precedence();
        string l = left.Precedence() > p ? left.PrettyPrint() : "(" + left.PrettyPrint() + ")";
        string r = right.Precedence() > p ? right.PrettyPrint() : "(" + right.PrettyPrint() + ")";
        return l + " " + op.PrettyPrint() + " " + r;
    }
    public int Precedence()
    {
        return op.Precedence();
    }
}
sealed record class ExpNode_Negate(ExpNode inner) : ExpNode
{
    public string PrettyPrint()
    {
        int p = Precedence();
        string str = inner.Precedence() > p ? inner.PrettyPrint() : "(" + inner.PrettyPrint() + ")";
        return "-" + str;
    }
    public int Precedence()
    {
        return 1000;
    }
}
sealed record class ExpNode_Num(BigFraction value) : ExpNode
{
    public string PrettyPrint()
    {
        return value.ToString();
    }
    public int Precedence()
    {
        return 1000000;
    }
}