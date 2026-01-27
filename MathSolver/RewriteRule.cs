interface RewriteRule
{
    public ExpNode Apply(ExpNode node);
    public bool CanApply(ExpNode node);
    public ExpNode ApplyRecursive(ExpNode node)
    {
        node.TransformChildren(ApplyRecursive);
        return Apply(node);
    }

    class CollapseNumbers : RewriteRule
    {
        public ExpNode Apply(ExpNode node)
        {
            if (node is ExpNode_Times t)
            {
                BigFraction val = new BigFraction(1, 1);
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val *= num.value;
                    }
                    else
                    {
                        return node;
                    }
                }
                return new ExpNode_Num(val);
            }
            if (node is ExpNode_Plus p)
            {
                BigFraction val = new BigFraction(1, 1);
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val += num.value;
                    }
                    else
                    {
                        return node;
                    }
                }
                return new ExpNode_Num(val);
            }
            return node;
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus;
        }
    }
}