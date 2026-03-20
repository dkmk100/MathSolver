// See https://aka.ms/new-console-template for more information

class Program
{
    public static void Main(string[] args)
    {
        TextReader reader = new StreamReader(args[0]);

        List<LexToken> tokens = Lexer.Tokenize(reader);

        //TODO proper error handling.
        RootNode tree = Parser.Parse(tokens);
        Console.WriteLine("input text: ");
        Console.Write(File.ReadAllText(args[0]));
        Console.WriteLine();
        Console.WriteLine("parsed: " + tree.PrettyPrint());

        RewriteRule[][] simplifyRules =
        [
            //initial simplification
            [
                new RewriteRule.CollapseNumbers(),
                new RewriteRule.DistributeTimes(),
            ],
            //continue rewrite after distributing
            [
                new RewriteRule.CollapseSymbolic(),
            ]
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
                }
            }
            else
            {
                Console.WriteLine("eval failed with errors: ");
                foreach (var err in eval.errors)
                {
                    Console.WriteLine(err.PrettyPrint());
                }
            }
        }
        else
        {
            Console.Error.WriteLine("unknown operation!");
            throw new Exception();
        }
    }
}
