// See https://aka.ms/new-console-template for more information

using System.Text;

class Program
{
    public static int Main(string[] args)
    {
        string mode = args[0];
        Stream reader;
        if (mode == "file")
        {
            reader = new FileStream(args[1], FileMode.Open);
        }
        else if (mode == "console")
        {
            if (args[1].StartsWith('"'))
            {
                if (args[1].EndsWith('"'))
                {
                    reader = new MemoryStream(Encoding.UTF8.GetBytes(args[1].Substring(1, args[1].Length - 2)));
                }
                else
                {
                    Console.Error.WriteLine("invalid input string: {0}", args[1]);
                    return -1;
                }
            }
            else
            {
                reader = new MemoryStream(Encoding.UTF8.GetBytes(args[1]));
            }
        }
        else
        {
            Console.Error.WriteLine("unknown input mode: " + mode);
            return -1;
        }

        Console.Write("input text: ");
        Console.Write(new StreamReader(reader).ReadToEnd());
        reader.Seek(0, SeekOrigin.Begin);
        Console.WriteLine();
        List<LexToken> tokens = Lexer.Tokenize(new StreamReader(reader));

        //TODO better error handling.
        RootNode tree;
        try
        {
            tree = Parser.Parse(tokens);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("parsing failed with error: ");
            Console.WriteLine(e.Message);
            return -1;
        }
        Console.WriteLine("parsed: " + tree.PrettyPrint());

        RewriteRule[] simplifyRules =
        [
            new RewriteRule.CollapseNumbers(),
            new RewriteRule.DistributeTimes(),
            new RewriteRule.CollapseSymbolic(),
        ];

        if (tree is RootNode_Simplify simplify)
        {
            ExpNode exp = simplify.inner;
            SolverResult<ExpNode> eval = MathEngine.RewriteRecursive(exp.CopyRecursive(), simplifyRules);

            if (eval.Success())
            {
                Console.WriteLine("eval: " + eval.result!.PrettyPrint());
            }
            else
            {
                Console.WriteLine("eval failed with errors: ");
                foreach (var err in eval.errors)
                {
                    Console.WriteLine(err.PrettyPrint());
                }
                return -1;
            }

        }
        else if (tree is RootNode_Solve solve)
        {
            SolverResult<ExpNode> eval = EquationSolver.Solve(solve);
            if (eval.Success())
            {
                Console.WriteLine("eval: {0} = {1}", solve.var, eval.result!.PrettyPrint());
                SolverResult<ExpNode> simplified = MathEngine.RewriteRecursive(eval.result!.CopyRecursive(), simplifyRules);

                if (simplified.Success())
                {
                    Console.WriteLine("simplified: {0} = {1}", solve.var, simplified.result!.PrettyPrint());
                }
                else
                {
                    Console.WriteLine("simplifying eval failed with errors: ");
                    foreach (var err in simplified.errors)
                    {
                        Console.WriteLine(err.PrettyPrint());
                    }
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("eval failed with errors: ");
                foreach (var err in eval.errors)
                {
                    Console.WriteLine(err.PrettyPrint());
                }
                return -1;
            }
        }
        else
        {
            Console.Error.WriteLine("unknown operation!");
            return -1;
        }
        return 0;
    }
}
