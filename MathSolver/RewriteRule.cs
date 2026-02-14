using System.Globalization;
using System.Runtime.CompilerServices;

interface RewriteRule
{
    public SolverResult Apply(ExpNode node);
    public bool CanApply(ExpNode node);
    public SolverResult ApplyRecursive(ExpNode node, bool failEarly)
    {
        SolverResult innerResult = node.TransformChildren(ApplyRecursive, failEarly);
        SolverResult result = new SolverResult(node);
        if (CanApply(node))
        {
            result = Apply(node);
            result.MergeErrors(innerResult);
        }
        return result;
    }

    class CollapseNumbers : RewriteRule
    {
        public SolverResult Apply(ExpNode node)
        {
            if (node is ExpNode_Invert inv)
            {
                if (inv.inner is ExpNode_Num num)
                {
                    if (num.value.IsZero)
                    {
                        return new SolverResult(new SolverError(SolverError.ErrorType.MathError, "Division by zero", inv));
                    }
                    var val = new BigFraction(num.value.Denominator(), num.value.Numerator());
                    return new SolverResult(new ExpNode_Num(val));
                }
                else
                {
                    return new SolverResult(node);
                }
            }
            else if (node is ExpNode_Times t)
            {
                BigFraction val = new BigFraction(1, 1);
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val *= num.value;
                    }
                    else
                    {
                        return new SolverResult(node);
                    }
                }
                return new SolverResult(new ExpNode_Num(val));
            }
            else if (node is ExpNode_Plus p)
            {
                BigFraction val = new BigFraction(0, 1);
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val += num.value;
                    }
                    else
                    {
                        return new SolverResult(node);
                    }
                }
                return new SolverResult(new ExpNode_Num(val));
            }
            return new SolverResult(node);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus;
        }
    }
}