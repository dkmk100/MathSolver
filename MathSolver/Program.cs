// See https://aka.ms/new-console-template for more information

class Program
{
    public static void Main(string[] args)
    {
        TextReader reader = new StreamReader(args[0]);

        List<LexToken> tokens = Lexer.Tokenize(reader);

        //TODO proper error handling.
        ExpNode tree = Parser.Parse(tokens);

        Console.WriteLine(tree.PrettyPrint());
    }
}
