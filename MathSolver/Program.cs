// See https://aka.ms/new-console-template for more information

class Program
{
    public static void Main(string[] args)
    {
        BigFraction f1 = new BigFraction(1, 3);
        BigFraction f2 = new BigFraction(1, 3);
        BigFraction sum = f1 / f2;
        Console.WriteLine("({0}) / ({1}) = {2}", f1, f2, sum);
        Console.WriteLine("Hello, World!");

        ExpNode node = new ExpNode_Negate(
            new ExpNode_BOp(BOperator.Times,
                new ExpNode_Num(f1),
                new ExpNode_Negate(new ExpNode_Num(f2))
            )
        );
        Console.WriteLine(node.PrettyPrint());
    }
}
