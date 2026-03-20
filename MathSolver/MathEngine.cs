using System.Numerics;


static class MathEngine
{
    //note they apply in the order listed
    public static SolverResult<ExpNode> RewriteSingle(ExpNode node, RewriteRule[] rules)
    {
        SolverResult<ExpNode> rslt = new SolverResult<ExpNode>(node);
        if (!rslt.Success()) { return rslt; }
        foreach (var rule in rules)
        {
            SolverResult<ExpNode> temp = rule.Apply(rslt.result!);
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
    public static SolverResult<ExpNode> RewriteRecursive(ExpNode node, RewriteRule[] rules)
    {
        SolverResult<ExpNode> rslt = new SolverResult<ExpNode>(node);
        rslt.MergeErrors(node.TransformChildren((inner, failEarly) => RewriteRecursive(inner, rules), true));
        if (!rslt.Success())
        {
            //fail early to allow reuse
            return rslt;
        }
        foreach (var rule in rules)
        {
            //apply rule to previous transformation
            SolverResult<ExpNode> temp = rule.Apply(rslt.result!);

            //save new transformed node
            temp.MergeErrors(rslt);
            rslt = temp;

            if (!rslt.Success())
            {
                return rslt;
            }

        }
        return rslt;
    }

    //perform a set of recursive rewrites in one go
    public static SolverResult<ExpNode> RewriteRecursive(ExpNode node, RewriteRule[][] ruleSets)
    {
        //TODO instead of this mess, consider making rewrite rule results indicate whether to re-recurse or not...
        //which would not only be cleaner code but would simplify more situations LOL
        var result = RewriteRecursive(node, ruleSets[0]);
        if (!result.Success()) { return result; }
        for (int i = 1; i < ruleSets.Length; i++)
        {
            //apply to previous result
            var temp = RewriteRecursive(result.result!, ruleSets[i]);
            temp.MergeErrors(result);
            result = temp;
        }
        return result;
    }
}