// See https://aka.ms/new-console-template for more information

class Program
{
    public static void Main(string[] args)
    {
        TextReader reader = new StreamReader(args[0]);

        List<LexToken> tokens = Lexer.Tokenize(reader);

        //TODO proper error handling.
        ExpNode tree = Parser.Parse(tokens);

        SolverResult eval = MathEngine.RewriteRecursive(tree.CopyRecursive(),
        [
            new RewriteRule.CollapseNumbers()
        ], true);//fail early to prevent error messages piling up

        Console.WriteLine("input text: ");
        Console.Write(File.ReadAllText(args[0]));
        Console.WriteLine();
        Console.WriteLine("parsed: " + tree.PrettyPrint());
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
}
