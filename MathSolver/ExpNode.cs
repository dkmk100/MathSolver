using System.Diagnostics.Tracing;
using System.Text;

interface ExpNode
{
    public string PrettyPrint();
    public int Precedence();
}

sealed class ExpNode_Pow(ExpNode left, ExpNode right) : ExpNode
{
    public string PrettyPrint()
    {
        int p = Precedence();
        string l = left.Precedence() > p ? left.PrettyPrint() : "(" + left.PrettyPrint() + ")";
        string r = right.Precedence() > p ? right.PrettyPrint() : "(" + right.PrettyPrint() + ")";
        return l + " ^ " + r;
    }
    public int Precedence()
    {
        return 300;
    }
}
sealed class ExpNode_Times(ExpNode[] nodes) : ExpNode
{

    public string PrettyPrint()
    {
        int p = Precedence();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < nodes.Length; i++)
        {
            var inner = nodes[i];
            string str = inner.Precedence() > p ? inner.PrettyPrint() : "(" + inner.PrettyPrint() + ")";
            sb.Append(str);
            if (i < nodes.Length - 1)
            {
                sb.Append(" * ");
            }
        }
        return sb.ToString();
    }
    public int Precedence()
    {
        return 200;
    }
}
sealed class ExpNode_Plus(ExpNode[] nodes) : ExpNode
{

    public string PrettyPrint()
    {
        int p = Precedence();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < nodes.Length; i++)
        {
            var inner = nodes[i];
            string str = inner.Precedence() > p ? inner.PrettyPrint() : "(" + inner.PrettyPrint() + ")";
            sb.Append(str);
            if (i < nodes.Length - 1)
            {
                sb.Append(" + ");
            }
        }
        return sb.ToString();
    }
    public int Precedence()
    {
        return 100;
    }
}
sealed record class ExpNode_Negate(ExpNode inner) : ExpNode
{
    public string PrettyPrint()
    {
        return "-(" + inner.PrettyPrint() + ")";
    }
    public int Precedence()
    {
        return 1000;
    }
}
sealed record class ExpNode_Invert(ExpNode inner) : ExpNode
{
    public string PrettyPrint()
    {
        return "(" + inner.PrettyPrint() + ")^-1";
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
        return 1000;
    }
}

sealed record class ExpNode_Var(string name, ExpNode? subscript) : ExpNode
{
    public string PrettyPrint()
    {
        return name + (subscript == null ? "" : "_(" + subscript.PrettyPrint() + ")");
    }
    public int Precedence()
    {
        return 1000;
    }
}