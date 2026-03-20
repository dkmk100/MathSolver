using System.Globalization;
using System.Runtime.CompilerServices;

interface RewriteRule
{
    public SolverResult<ExpNode> Apply(ExpNode node);
    public bool CanApply(ExpNode node);

    //TODO can this be removed? MathEngine has a strictly better version lol
    public SolverResult<ExpNode> ApplyRecursive(ExpNode node, bool failEarly)
    {
        SolverResult<ExpNode> innerResult = node.TransformChildren(ApplyRecursive, failEarly);
        SolverResult<ExpNode> result = new SolverResult<ExpNode>(node);
        if (CanApply(node))
        {
            result = Apply(node);
            result.MergeErrors(innerResult);
        }
        return result;
    }



    class CollapseNumbers : RewriteRule
    {
        public SolverResult<ExpNode> Apply(ExpNode node)
        {
            if (node is ExpNode_Invert inv)
            {
                if (inv.inner is ExpNode_Num num)
                {
                    if (num.value.IsZero)
                    {
                        return new SolverResult<ExpNode>(new SolverError(SolverError.ErrorType.MathError, "Division by zero", inv));
                    }
                    var val = new BigFraction(num.value.Denominator(), num.value.Numerator());
                    return new SolverResult<ExpNode>(new ExpNode_Num(val));
                }
                else
                {
                    return new SolverResult<ExpNode>(node);
                }
            }
            else if (node is ExpNode_Times t)
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
                        return new SolverResult<ExpNode>(node);
                    }
                }
                return new SolverResult<ExpNode>(new ExpNode_Num(val));
            }
            else if (node is ExpNode_Plus p)
            {
                BigFraction val = new BigFraction(0, 1);
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val += num.value;
                    }
                    else
                    {
                        return new SolverResult<ExpNode>(node);
                    }
                }
                return new SolverResult<ExpNode>(new ExpNode_Num(val));
            }
            return new SolverResult<ExpNode>(node);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus || node is ExpNode_Invert;
        }
    }
    class DistributeTimes : RewriteRule
    {
        public SolverResult<ExpNode> Apply(ExpNode node)
        {
            if (node is ExpNode_Times times)
            {
                List<ExpNode_Plus> sums = new List<ExpNode_Plus>();
                List<ExpNode> others = new List<ExpNode>();
                foreach (var child in times.nodes)
                {
                    if (child is ExpNode_Plus p)
                    {
                        sums.Add(p);
                    }
                    else
                    {
                        others.Add(child);
                    }
                }

                if (sums.Count == 0)
                {
                    //nothing to distribute lol
                    return new SolverResult<ExpNode>(node);
                }
                //TODO consider heuristic for distributing sums or similar? IDK what to do here TBH
                else if (sums.Count == 1)
                {
                    ExpNode_Plus target = sums[0];
                    //create distributed multiplications
                    List<ExpNode> transformed = new List<ExpNode>();
                    foreach (var child in target.nodes)
                    {
                        List<ExpNode> inner = new List<ExpNode>([child]);
                        foreach (var other in others)
                        {
                            inner.Add(other.CopyRecursive());
                        }
                        transformed.Add(ExpNode_Times.Collapsed(inner));
                    }

                    //return as new addition
                    return new SolverResult<ExpNode>(new ExpNode_Plus(transformed.ToArray()));
                }
                else
                {
                    Console.Error.WriteLine("Warning: failed to distribute multiplication: {0}", node.PrettyPrint());
                    return new SolverResult<ExpNode>(node);
                }
            }
            else if (node is ExpNode_Negate neg)
            {
                if (neg.inner is ExpNode_Plus innerPlus)
                {
                    List<ExpNode> transformed = new List<ExpNode>();
                    foreach (var child in innerPlus.nodes)
                    {
                        transformed.Add(ExpNode_Negate.Collapsed(child));
                    }
                    //return as new addition
                    return new SolverResult<ExpNode>(new ExpNode_Plus(transformed.ToArray()));
                }
            }
            return new SolverResult<ExpNode>(node);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times;
        }
    }


    //the big brother of CollapseNumbers
    //collapses variables and such
    //note this DOESN'T do the number stuff too; you should run that one first
    class CollapseSymbolic : RewriteRule
    {
        public SolverResult<ExpNode> Apply(ExpNode node)
        {
            if (node is ExpNode_Times t)
            {
                List<ExpNode> plainChildren = new List<ExpNode>();
                Dictionary<string, int> varChildren = new Dictionary<string, int>();
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Num num && num.value.IsOne)
                    {
                        //multiplication by one is a no-op
                    }
                    else if (child is ExpNode_Var v)
                    {
                        if (varChildren.ContainsKey(v.name))
                        {
                            varChildren[v.name] += 1;
                        }
                        else
                        {
                            varChildren.Add(v.name, 1);
                        }
                    }
                    else if (child is ExpNode_Invert inv)
                    {
                        if (inv.inner is ExpNode_Var innerVar)
                        {
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] -= 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, -1);
                            }
                        }
                        else
                        {
                            plainChildren.Add(child);
                        }
                    }
                    else
                    {
                        plainChildren.Add(child);
                    }
                }

                //merge children and return
                foreach (var (v, count) in varChildren)
                {
                    if (count == 0)
                    {
                        //cancel out, nothing to do here
                    }
                    if (count == 1)
                    {
                        //add back variable
                        plainChildren.Add(new ExpNode_Var(v, null));
                    }
                    else if (count == -1)
                    {
                        //add back 1/variable
                        plainChildren.Add(new ExpNode_Invert(new ExpNode_Var(v, null)));
                    }
                    else
                    {
                        //add in v ^ count
                        plainChildren.Add(new ExpNode_Pow(new ExpNode_Var(v, null), new ExpNode_Num(new BigFraction(count, 1))));
                    }
                }
                return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(plainChildren));
            }
            else if (node is ExpNode_Plus p)
            {
                List<ExpNode> plainChildren = new List<ExpNode>();
                Dictionary<string, int> varChildren = new Dictionary<string, int>();
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Num num && num.value.IsZero)
                    {
                        //addition by zero is a no-op
                    }
                    else if (child is ExpNode_Var v)
                    {
                        if (varChildren.ContainsKey(v.name))
                        {
                            varChildren[v.name] += 1;
                        }
                        else
                        {
                            varChildren.Add(v.name, 1);
                        }
                    }
                    else if (child is ExpNode_Negate neg)
                    {
                        if (neg.inner is ExpNode_Var innerVar)
                        {
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] -= 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, -1);
                            }
                        }
                        else
                        {
                            plainChildren.Add(child);
                        }
                    }
                    else
                    {
                        plainChildren.Add(child);
                    }
                }

                //merge children and return
                foreach (var (v, count) in varChildren)
                {
                    if (count == 0)
                    {
                        //cancel out, nothing to do here
                    }
                    if (count == 1)
                    {
                        //add back variable
                        plainChildren.Add(new ExpNode_Var(v, null));
                    }
                    else if (count == -1)
                    {
                        //add back negated variable
                        plainChildren.Add(new ExpNode_Negate(new ExpNode_Var(v, null)));
                    }
                    else
                    {
                        //add in count * v
                        plainChildren.Add(new ExpNode_Times([new ExpNode_Num(new BigFraction(count, 1)), new ExpNode_Var(v, null)]));
                    }
                }
                return new SolverResult<ExpNode>(ExpNode_Plus.Collapsed(plainChildren));
            }
            else if (node is ExpNode_Negate neg)
            {
                //ok I'm not 100% sure what to do here... 
                //I'm starting to regret having a dedicated negate node instead of just multiplying by -1...
                if (neg.inner is ExpNode_Times innerTimes)
                {
                    List<ExpNode> children = [new ExpNode_Num(new BigFraction(-1, 1)), .. innerTimes.nodes];
                    return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(children));
                }
            }
            return new SolverResult<ExpNode>(node);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus || node is ExpNode_Negate;
        }
    }

}