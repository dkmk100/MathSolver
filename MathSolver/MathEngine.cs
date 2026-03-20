using System.Numerics;
using System.Reflection;


static class MathEngine
{
    //note they apply in the order listed
    public static SolverResult<ExpNode> RewriteSingle(ExpNode node, RewriteRule[] rules)
    {
        SolverResult<ExpNode> rslt = new SolverResult<ExpNode>(node, false, false);
        if (!rslt.Success()) { return rslt; }
        foreach (var rule in rules)
        {
            SolverResult<ExpNode> temp = rule.Apply(rslt.result!);
            temp.MergePeerStatus(rslt);
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
        SolverResult<ExpNode> rslt = new SolverResult<ExpNode>(node, false, false);
        bool pendingChanges = true;
        bool recurse = true;
        bool transformed = false;
        bool transformedChildren = false;
        //TODO add a timeout to prevent freezing
        while (pendingChanges)
        {
            if (recurse)
            {
                rslt.MergePeerStatus(rslt.result!.TransformChildren((inner, failEarly) => RewriteRecursive(inner, rules), true));
            }
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
                temp.MergePeerStatus(rslt);
                rslt = temp;

                if (!rslt.Success())
                {
                    return rslt;
                }
            }

            //re-apply rules recursively after transformations
            recurse = rslt.transformedChildren;
            pendingChanges = rslt.transformed || recurse;
            //store results, then reset to prevent infinite loop
            if (rslt.transformed)
            {
                transformed = true;
            }
            if (rslt.transformedChildren)
            {
                transformedChildren = true;
            }
            rslt.transformed = false;
            rslt.transformedChildren = false;
        }
        //restore transformation status
        rslt.transformed = transformed;
        rslt.transformedChildren = transformedChildren;
        return rslt;
    }
}