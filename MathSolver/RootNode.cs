interface RootNode : TreeNode<RootNode, ExpNode>
{

}

sealed class RootNode_Solve : RootNode
{
    public ExpNode left;
    public ExpNode right;
    public string var;
    public RootNode_Solve(ExpNode left, ExpNode right, string var)
    {
        this.left = left;
        this.right = right;
        this.var = var;
    }
    public IEnumerable<ExpNode> Children()
    {
        yield return left;
        yield return right;
    }

    public RootNode CopyRecursive()
    {
        return new RootNode_Solve(left.CopyRecursive(), right.CopyRecursive(), var);
    }

    public int Precedence()
    {
        return 0;
    }

    public string PrettyPrint()
    {
        return "Solve{" + left.PrettyPrint() + "=" + right.PrettyPrint() + "," + var + "}";
    }

    public SolverResult<RootNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<RootNode> result = new SolverResult<RootNode>(this, false, false);
        SolverResult<ExpNode> temp;

        temp = map(left, failEarly);
        result.MergeChildStatus(temp);
        if (temp.Success())
        {
            left = temp.result!;
        }
        else if (failEarly)
        {
            return result;
        }

        temp = map(right, failEarly);
        result.MergeChildStatus(temp);
        if (temp.Success())
        {
            right = temp.result!;
        }
        else if (failEarly)
        {
            return result;
        }

        return result;
    }
}

sealed class RootNode_Simplify : RootNode
{
    public ExpNode inner;
    public RootNode_Simplify(ExpNode inner)
    {
        this.inner = inner;
    }
    public IEnumerable<ExpNode> Children()
    {
        yield return inner;
    }

    public RootNode CopyRecursive()
    {
        return new RootNode_Simplify(inner.CopyRecursive());
    }

    public int Precedence()
    {
        return 0;
    }

    public string PrettyPrint()
    {
        return "Simplify{" + inner.PrettyPrint() + "}";
    }

    public SolverResult<RootNode> TransformChildren(Func<ExpNode, bool, SolverResult<ExpNode>> map, bool failEarly)
    {
        SolverResult<RootNode> result = new SolverResult<RootNode>(this, false, false);
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
}