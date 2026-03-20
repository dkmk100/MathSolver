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
        VarFinder finder = new VarFinder();
        //scan vars on left tree
        SolverResult<Dictionary<ExpNode, HashSet<string>>> varScan = ((TreeScanner<HashSet<string>>)finder).ScanRecursive(root.left, true);
        if (!varScan.Success())
        {
            return new SolverResult<ExpNode>().MergePeerStatus(varScan);
        }
        //scan right tree in on same dictionary, prevents annoying duplication later
        varScan = ((TreeScanner<HashSet<string>>)finder).ScanRecursive(root.right, true, varScan.result!);
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

        bool inLeft = vars[root.left].Contains(root.var);
        bool inRight = vars[root.right].Contains(root.var);
        ExpNode source;
        ExpNode dest;

        //TODO consolidate variables to one side if necessary
        if (inLeft && inRight)
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.SolveError, "Cannot consolidate variable on both sides of equation", null));
        }
        else if (inLeft)
        {
            source = root.left;
            dest = root.right;
        }
        else if (inRight)
        {
            source = root.right;
            dest = root.left;
        }
        else
        {
            return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.SolveError, "Variable not found in equation", null));
        }
        return SolveSimple(root.left, root.right, root.var, vars, null);
    }
    private static SolverResult<ExpNode> SolveSimple(ExpNode source, ExpNode dest, string var, Dictionary<ExpNode, HashSet<string>> vars, ErrorSource? parent)
    {
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