using System.Globalization;
using System.Runtime.CompilerServices;
using System.Transactions;

interface RewriteRule
{
    public SolverResult<ExpNode> Apply(ExpNode node);
    public bool CanApply(ExpNode node);

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
                    return new SolverResult<ExpNode>(new ExpNode_Num(val), true, false);
                }
                else
                {
                    return new SolverResult<ExpNode>(node, false, false);
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
                        return new SolverResult<ExpNode>(node, false, false);
                    }
                }
                return new SolverResult<ExpNode>(new ExpNode_Num(val), true, false);
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
                        return new SolverResult<ExpNode>(node, false, false);
                    }
                }
                return new SolverResult<ExpNode>(new ExpNode_Num(val), true, false);
            }
            return new SolverResult<ExpNode>(node, false, false);
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
                    return new SolverResult<ExpNode>(node, false, false);
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
                    return new SolverResult<ExpNode>(new ExpNode_Plus(transformed.ToArray()), true, true);
                }
                else
                {
                    Console.Error.WriteLine("Warning: failed to distribute multiplication: {0}", node.PrettyPrint());
                    return new SolverResult<ExpNode>(node, false, false);
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
                    return new SolverResult<ExpNode>(new ExpNode_Plus(transformed.ToArray()), true, true);
                }
            }
            return new SolverResult<ExpNode>(node, false, false);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Invert;
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
                bool negate = false;
                int numSwaps = 0;
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        if (num.value.IsOne)
                        {
                            //multiplication by one is a no-op
                        }
                        else
                        {
                            if (num.value == new BigFraction(-1, 1))
                            {
                                negate = !negate;
                                numSwaps += 1;
                            }
                            plainChildren.Add(child);
                        }

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

                if (numSwaps == t.nodes.Length - 1)
                {
                    //special case: negating an otherwise plain multiplication
                    foreach (var child in t.nodes)
                    {
                        if (child is ExpNode_Num n)
                        {
                            if (n.value != new BigFraction(-1, 1))
                            {
                                if (negate)
                                {
                                    return new SolverResult<ExpNode>(ExpNode_Negate.Collapsed(child), true, true);
                                }
                                else
                                {
                                    return new SolverResult<ExpNode>(child, true, false);
                                }
                            }
                        }
                        else
                        {
                            if (negate)
                            {
                                return new SolverResult<ExpNode>(ExpNode_Negate.Collapsed(child), true, true);
                            }
                            else
                            {
                                return new SolverResult<ExpNode>(child, true, false);
                            }
                        }
                    }
                }

                //merge children and return
                bool changed = false;
                foreach (var (v, count) in varChildren)
                {
                    if (count == 0)
                    {
                        changed = true;
                        //cancel out, nothing to do here
                    }
                    else if (count == 1)
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
                        changed = true;
                        //add in v ^ count
                        plainChildren.Add(new ExpNode_Pow(new ExpNode_Var(v, null), new ExpNode_Num(new BigFraction(count, 1))));
                    }
                }

                return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(plainChildren), changed, changed);
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
                bool changed = false;
                foreach (var (v, count) in varChildren)
                {
                    if (count == 0)
                    {
                        changed = true;
                        //cancel out, nothing to do here
                    }
                    else if (count == 1)
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
                        changed = true;
                        //add in count * v
                        plainChildren.Add(new ExpNode_Times([new ExpNode_Num(new BigFraction(count, 1)), new ExpNode_Var(v, null)]));
                    }
                }
                return new SolverResult<ExpNode>(ExpNode_Plus.Collapsed(plainChildren), changed, changed);
            }
            else if (node is ExpNode_Negate neg)
            {
                //ok I'm not 100% sure what to do here... 
                //I'm starting to regret having a dedicated negate node instead of just multiplying by -1...
                if (neg.inner is ExpNode_Times innerTimes)
                {
                    List<ExpNode> children = [new ExpNode_Num(new BigFraction(-1, 1)), .. innerTimes.nodes];
                    return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(children), true, true);
                }
            }
            return new SolverResult<ExpNode>(node, false, false);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus || node is ExpNode_Negate;
        }
    }

}