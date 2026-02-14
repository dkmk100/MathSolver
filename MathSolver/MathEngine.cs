using System.Numerics;


static class MathEngine
{
    //note they apply in the order listed
    public static SolverResult RewriteSingle(ExpNode node, RewriteRule[] rules)
    {
        SolverResult rslt = new SolverResult(node);
        foreach (var rule in rules)
        {
            SolverResult temp = rule.Apply(node);
            temp.MergeErrors(rslt);
            if (!temp.Success())
            {
                return rslt;
            }
            //save new transformed node
            rslt = temp;
        }
        return rslt;
    }

    //this is not very performant, but works as a simple starting point
    public static SolverResult RewriteRecursive(ExpNode node, RewriteRule[] rules, bool failEarly)
    {
        SolverResult rslt = new SolverResult(node);
        rslt.MergeErrors(node.TransformChildren((inner, failEarly) => RewriteRecursive(inner, rules, failEarly), failEarly));
        if (!rslt.Success() && failEarly)
        {
            //fail early to prevent error messages from building up
            return rslt;
        }
        foreach (var rule in rules)
        {
            SolverResult temp = rule.Apply(node);

            //save new transformed node
            temp.MergeErrors(rslt);
            rslt = temp;

            if (!rslt.Success() && failEarly)
            {
                return rslt;
            }

        }
        return rslt;
    }
}