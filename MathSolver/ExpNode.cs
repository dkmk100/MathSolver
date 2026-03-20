using System.Diagnostics.Tracing;
using System.Runtime.InteropServices.Swift;
using System.Text;

interface ExpNode : TreeNode<ExpNode, ExpNode>
{
    //methods are inherited from TreeNode
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        SolverResult<ExpNode> l = map(left, failEarly);
        result.MergeChildStatus(l);
        if (!l.Success() && failEarly)
        {
            return result;
        }
        SolverResult<ExpNode> r = map(right, failEarly);
        result.MergeChildStatus(r);
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
    //collapse inner expressions, used when transforming trees
    public static ExpNode Collapsed(List<ExpNode> nodes)
    {
        if (nodes.Count == 1)
        {
            //skip unnecessary wrapper
            return nodes[0];
        }
        List<ExpNode> targets = new List<ExpNode>();
        foreach (var inner in nodes)
        {
            //collapse only one layer; anything deeper is the responsibility of whatever build those nodes
            if (inner is ExpNode_Times nested)
            {
                targets.Add(nested);
            }
        }
        foreach (var inner in targets)
        {
            //transform node list
            //TODO switch to indexes to make this less painfully slow
            nodes.Remove(inner);
            nodes.AddRange(inner.Children());
        }
        return new ExpNode_Times(nodes.ToArray());
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        for (int i = 0; i < nodes.Length; i++)
        {
            SolverResult<ExpNode> temp = map(nodes[i], failEarly);
            result.MergeChildStatus(temp);
            if (temp.Success())
            {
                nodes[i] = temp.result!;
            }
            else if (failEarly)
            {
                return result;
            }
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

    //collapse inner plus expressions, used when transforming trees
    public static ExpNode Collapsed(List<ExpNode> nodes)
    {
        if (nodes.Count == 1)
        {
            //skip unnecessary wrapper
            return nodes[0];
        }
        List<ExpNode> sums = new List<ExpNode>();
        foreach (var inner in nodes)
        {
            //collapse only one layer; anything deeper is the responsibility of whatever build those nodes
            if (inner is ExpNode_Plus p)
            {
                sums.Add(p);
            }
        }
        foreach (var inner in sums)
        {
            //transform node list
            //TODO switch to indexes to make this less painfully slow
            nodes.Remove(inner);
            nodes.AddRange(inner.Children());
        }
        return new ExpNode_Plus(nodes.ToArray());
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        for (int i = 0; i < nodes.Length; i++)
        {
            SolverResult<ExpNode> temp = map(nodes[i], failEarly);
            result.MergeChildStatus(temp);
            if (temp.Success())
            {
                nodes[i] = temp.result!;
            }
            else if (failEarly)
            {
                return result;
            }
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
    //collapse inner expressions, used when transforming trees
    public static ExpNode Collapsed(ExpNode inner)
    {
        if (inner is ExpNode_Negate n)
        {
            return n.inner;
        }
        else
        {
            return new ExpNode_Negate(inner);
        }
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        SolverResult<ExpNode> temp = map(inner, failEarly);
        result.MergeChildStatus(temp);
        if (temp.Success())
        {
            inner = temp.result!;
        }
        else if (failEarly)
        {
            return result;
        }
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
    //collapse inner expressions, used when transforming trees
    public static ExpNode Collapsed(ExpNode inner)
    {
        if (inner is ExpNode_Invert nested)
        {
            return nested.inner;
        }
        else
        {
            return new ExpNode_Invert(inner);
        }
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        SolverResult<ExpNode> temp = map(inner, failEarly);
        result.MergeChildStatus(temp);
        if (temp.Success())
        {
            inner = temp.result!;
        }
        else if (failEarly)
        {
            return result;
        }
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
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
        //TODO figure out if this support can be added or if I should remove this...
        if (subscript != null)
        {
            throw new NotImplementedException();
        }
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
    public SolverResult<ExpNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(this, false, false);
        if (subscript != null)
        {
            SolverResult<ExpNode> temp = map(subscript, failEarly);
            result.MergeChildStatus(temp);
            if (temp.Success())
            {
                subscript = temp.result!;
            }
            else if (failEarly)
            {
                return result;
            }

        }
        return result;
    }
    public ExpNode CopyRecursive()
    {
        return new ExpNode_Var(name, subscript?.CopyRecursive());
    }
}