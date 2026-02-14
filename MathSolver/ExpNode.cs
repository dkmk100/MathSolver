using System.Diagnostics.Tracing;
using System.Text;

interface ExpNode
{
    public string PrettyPrint();
    public int Precedence();
    public IEnumerable<ExpNode> Children();
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly);
    public ExpNode CopyRecursive();
}

sealed class ExpNode_Pow : ExpNode
{
    public ExpNode left;
    public ExpNode right;
    public ExpNode_Pow(ExpNode left, ExpNode right)
    {
        this.left = left;
        this.right = right;
    }
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
    public IEnumerable<ExpNode> Children()
    {
        yield return left;
        yield return right;
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        SolverResult l = map(left, failEarly);
        result.MergeErrors(l);
        if (!l.Success() && failEarly)
        {
            return result;
        }
        SolverResult r = map(right, failEarly);
        result.MergeErrors(r);
        if (!r.Success() && failEarly)
        {
            return result;
        }

        //actually apply transformations
        left = l.result!;
        right = r.result!;

        //return value
        return result;
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Pow(left.CopyRecursive(), right.CopyRecursive());
    }
}
sealed class ExpNode_Times : ExpNode
{
    public ExpNode[] nodes;
    public ExpNode_Times(ExpNode[] nodes)
    {
        this.nodes = nodes;
    }
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
    public IEnumerable<ExpNode> Children()
    {
        foreach (var child in nodes)
        {
            yield return child;
        }
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        for (int i = 0; i < nodes.Length; i++)
        {
            SolverResult temp = map(nodes[i], failEarly);
            result.MergeErrors(temp);
            if (!temp.Success() && failEarly)
            {
                return result;
            }
            nodes[i] = temp.result!;
        }
        return result;
    }
    public ExpNode CopyRecursive()
    {
        ExpNode[] newNodes = new ExpNode[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            newNodes[i] = nodes[i].CopyRecursive();
        }
        return new ExpNode_Times(newNodes);
    }
}
sealed class ExpNode_Plus : ExpNode
{
    public ExpNode[] nodes;
    public ExpNode_Plus(ExpNode[] nodes)
    {
        this.nodes = nodes;
    }

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
    public IEnumerable<ExpNode> Children()
    {
        foreach (var child in nodes)
        {
            yield return child;
        }
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        for (int i = 0; i < nodes.Length; i++)
        {
            SolverResult temp = map(nodes[i], failEarly);
            result.MergeErrors(temp);
            if (!temp.Success() && failEarly)
            {
                return result;
            }
            nodes[i] = temp.result!;
        }
        return result;
    }
    public ExpNode CopyRecursive()
    {
        ExpNode[] newNodes = new ExpNode[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            newNodes[i] = nodes[i].CopyRecursive();
        }
        return new ExpNode_Plus(newNodes);
    }
}
sealed class ExpNode_Negate : ExpNode
{
    public ExpNode inner;
    public ExpNode_Negate(ExpNode inner)
    {
        this.inner = inner;
    }
    public string PrettyPrint()
    {
        return "-(" + inner.PrettyPrint() + ")";
    }
    public int Precedence()
    {
        return 1000;
    }
    public IEnumerable<ExpNode> Children()
    {
        yield return inner;
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        SolverResult temp = map(inner, failEarly);
        result.MergeErrors(temp);
        if (!temp.Success() && failEarly)
        {
            return result;
        }
        inner = temp.result!;
        return result;
    }
    public void TransformChildren(Func<ExpNode, ExpNode> map)
    {
        inner = map(inner);
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Negate(inner.CopyRecursive());
    }
}
sealed class ExpNode_Invert : ExpNode
{
    public ExpNode inner;
    public ExpNode_Invert(ExpNode inner)
    {
        this.inner = inner;
    }
    public string PrettyPrint()
    {
        return "(" + inner.PrettyPrint() + ")^-1";
    }
    public int Precedence()
    {
        return 1000;
    }
    public IEnumerable<ExpNode> Children()
    {
        yield return inner;
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        SolverResult temp = map(inner, failEarly);
        result.MergeErrors(temp);
        if (!temp.Success() && failEarly)
        {
            return result;
        }
        inner = temp.result!;
        return result;
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Invert(inner.CopyRecursive());
    }
}
sealed record class ExpNode_Num : ExpNode
{
    public BigFraction value;
    public ExpNode_Num(BigFraction value)
    {
        this.value = value;
    }
    public string PrettyPrint()
    {
        return value.ToString();
    }
    public int Precedence()
    {
        return 1000;
    }
    public IEnumerable<ExpNode> Children()
    {
        return Enumerable.Empty<ExpNode>();
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        //nothing to do here lol, no children!
        return result;
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Num(value);
    }
}

sealed class ExpNode_Var : ExpNode
{
    public string name;
    public ExpNode? subscript;
    public ExpNode_Var(string name, ExpNode? subscript)
    {
        this.name = name;
        this.subscript = subscript;
    }
    public string PrettyPrint()
    {
        return name + (subscript == null ? "" : "_(" + subscript.PrettyPrint() + ")");
    }
    public int Precedence()
    {
        return 1000;
    }
    public IEnumerable<ExpNode> Children()
    {
        if (subscript != null)
        {
            yield return subscript;
        }
    }
    public SolverResult TransformChildren(Func<ExpNode, bool, SolverResult> map, bool failEarly)
    {
        SolverResult result = new SolverResult(this);
        if (subscript != null)
        {
            SolverResult temp = map(subscript, failEarly);
            result.MergeErrors(temp);
            if (!temp.Success() && failEarly)
            {
                return result;
            }
            subscript = temp.result!;
        }
        return result;
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Var(name, subscript?.CopyRecursive());
    }
}