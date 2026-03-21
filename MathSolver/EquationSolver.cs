using System.Text;

class EquationSolver
{
    private static void PrintAnnotations<T>(ExpNode tree, Dictionary<ExpNode, T> dict, Func<T, string> printer) where T : class
    {
        Stack<ExpNode> nodes = new Stack<ExpNode>();
        nodes.Push(tree);
        while (nodes.Count > 0)
        {
            ExpNode node = nodes.Pop();
            foreach (var child in node.Children())
            {
                nodes.Push(child);
            }
            T data = dict[node];
            Console.WriteLine("{0}: {1}", node.PrettyPrint(), printer(data));
        }
    }

    //destructive on the root node, like other transformers
    public static SolverResult<ExpNode> Solve(RootNode_Solve root)
    {
        RewriteRule[] simplifyRules =
        [
            new RewriteRule.CollapseNumbers(),
            new RewriteRule.DistributeTimes(),
            new RewriteRule.CollapseSymbolic(),
        ];

        //start by simplifying expressions to group like terms
        //necessary because solver doesn't like vars in multiple places
        var left = MathEngine.RewriteRecursive(root.left, simplifyRules);
        if (!left.Success()) { return left; }
        var right = MathEngine.RewriteRecursive(root.right, simplifyRules);
        if (!right.Success()) { return right; }

        //TODO instead of just simplifying, I should just use a separate internal representation for equation solving
        //a structure that enforces more rigidity and less ambiguity

        VarFinder finder = new VarFinder();
        //scan vars on left tree
        SolverResult<Dictionary<ExpNode, HashSet<string>>> varScan = ((TreeScanner<HashSet<string>>)finder).ScanRecursive(left.result!, true);
        if (!varScan.Success())
        {
            return new SolverResult<ExpNode>().MergePeerStatus(varScan);
        }
        //scan right tree in on same dictionary, prevents annoying duplication later
        varScan = ((TreeScanner<HashSet<string>>)finder).ScanRecursive(right.result!, true, varScan.result!);
        if (!varScan.Success())
        {
            return new SolverResult<ExpNode>().MergePeerStatus(varScan);
        }
        Dictionary<ExpNode, HashSet<string>> vars = varScan.result!;

        //some (temporary) debug info
        /*
        Console.WriteLine("Target variable: {0}", root.var);
        Console.WriteLine("Expression vars breakdown: ");
        PrintAnnotations(root.left, vars, (names) =>
        {
            StringBuilder builder = new StringBuilder();
            foreach (var name in names.Order())
            {
                builder.Append(name);
                builder.Append(", ");
            }
            return builder.ToString();
        });
        */



        bool inLeft = vars[left.result!].Contains(root.var);
        bool inRight = vars[right.result!].Contains(root.var);
        ExpNode source;
        ExpNode dest;

        //TODO consolidate variables to one side if necessary
        if (inLeft && inRight)
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.SolveError, "Cannot consolidate variable on both sides of equation", null));
        }
        else if (inLeft)
        {
            source = left.result!;
            dest = right.result!;
        }
        else if (inRight)
        {
            source = right.result!;
            dest = left.result!;
        }
        else
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.SolveError, "Variable not found in equation", null));
        }

        return SolveSimple(source, dest, root.var, vars, null);
    }
    private static SolverResult<ExpNode> SolveSimple(ExpNode source, ExpNode dest, string var, Dictionary<ExpNode, HashSet<string>> vars, ErrorSource? parent)
    {
        //TODO track branching and invalid values, eg. stemming from division

        if (source is ExpNode_Var v)
        {
            //equation is solved
            return new SolverResult<ExpNode>(dest, false, false);
        }
        else if (source is ExpNode_Plus plus)
        {
            return SolveSimpleGrouped(source, dest, var, vars, parent, ExpNode_Negate.Collapsed, ExpNode_Plus.Collapsed);
        }
        else if (source is ExpNode_Times times)
        {
            return SolveSimpleGrouped(source, dest, var, vars, parent, ExpNode_Invert.Collapsed, ExpNode_Times.Collapsed);
        }
        else if (source is ExpNode_Negate neg)
        {
            var node = ExpNode_Negate.Collapsed(dest);
            HashSet<string> newVars = [.. vars[dest]];
            vars.Add(node, newVars);
            return SolveSimple(neg.inner, node, var, vars, neg);
        }
        else if (source is ExpNode_Invert inv)
        {
            var node = ExpNode_Invert.Collapsed(dest);
            HashSet<string> newVars = [.. vars[dest]];
            vars.Add(node, newVars);
            return SolveSimple(inv.inner, node, var, vars, inv);
        }
        else
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.InternalError, "Unknown expression type", source));
        }
    }
    private static SolverResult<ExpNode> SolveSimpleGrouped(ExpNode source, ExpNode dest, string var,
        Dictionary<ExpNode, HashSet<string>> vars, ErrorSource? parent, Func<ExpNode, ExpNode> inverse, Func<List<ExpNode>, ExpNode> regroup)
    {
        List<ExpNode> innerSources = new List<ExpNode>();
        foreach (ExpNode child in source.Children())
        {
            if (vars[child].Contains(var))
            {
                innerSources.Add(child);
            }
        }
        if (innerSources.Count > 1)
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.SolveError, "Cannot consolidate expression", source));
        }
        else
        {
            ExpNode inner = innerSources[0];
            List<ExpNode> swaps = new List<ExpNode>();
            //build new plus 
            HashSet<string> newVars = new HashSet<string>();
            swaps.Add(dest);
            foreach (var name in vars[dest])
            {
                newVars.Add(name);
            }
            foreach (var child in source.Children())
            {
                if (child != inner)
                {
                    swaps.Add(inverse(child));
                    foreach (var name in vars[child])
                    {
                        newVars.Add(name);
                    }
                }
            }
            ExpNode outer = regroup(swaps);
            //update vars scan to prevent breaking things
            //TODO is this necessary? do we need a list of vars on the dest?
            vars.Add(outer, newVars);
            //recusively solve the equation
            return SolveSimple(inner, outer, var, vars, source);
        }
    }
}