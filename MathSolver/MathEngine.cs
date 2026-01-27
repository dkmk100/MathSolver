using System.Numerics;


static class MathEngine
{
    //note they apply in the order listed
    public static ExpNode RewriteSingle(ExpNode node, RewriteRule[] rules)
    {
        foreach (var rule in rules)
        {
            node = rule.Apply(node);
        }
        return node;
    }
    public static ExpNode RewriteRecursive(ExpNode node, RewriteRule[] rules)
    {
        node.TransformChildren((inner) => RewriteRecursive(inner, rules));
        foreach (var rule in rules)
        {
            node = rule.Apply(node);
        }
        return node;
    }
}