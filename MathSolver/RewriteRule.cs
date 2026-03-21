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
                else if (inv.inner is ExpNode_Pow innerPow)
                {
                    //we know the inner value isn't a number; if it
                    ExpNode right;
                    if (innerPow.right is ExpNode_Num innerNum)
                    {
                        right = new ExpNode_Num(innerNum.value * new BigFraction(-1, 1));
                    }
                    else
                    {
                        right = new ExpNode_Times([new ExpNode_Num(new BigFraction(-1, 1)), innerPow.right]);
                    }
                    return new SolverResult<ExpNode>(new ExpNode_Pow(innerPow.left, right), true, false);
                }
                else
                {
                    return new SolverResult<ExpNode>(node, false, false);
                }
            }
            if (node is ExpNode_Negate neg)
            {
                if (neg.inner is ExpNode_Num num)
                {
                    var val = new BigFraction(-1 * num.value.Numerator(), num.value.Denominator());
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
                List<ExpNode> others = new List<ExpNode>();
                bool transformed = false;
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val *= num.value;
                    }
                    else if (child is ExpNode_Negate innerNeg)
                    {
                        val *= new BigFraction(-1, 1);
                        others.Add(innerNeg.inner);
                        transformed = true;
                    }
                    else
                    {
                        others.Add(child);
                    }
                }
                if (others.Count == 0)
                {
                    return new SolverResult<ExpNode>(new ExpNode_Num(val), true, false);
                }
                else if (others.Count >= t.nodes.Length - 1 && !transformed)
                {
                    return new SolverResult<ExpNode>(node, false, false);
                }
                else
                {
                    //negation is a special case
                    if (val == new BigFraction(-1, 1) && others.Count == 1)
                    {
                        return new SolverResult<ExpNode>(new ExpNode_Negate(others[0]), true, false);
                    }
                    //multiplying by one is a no-op
                    else if (!val.IsOne)
                    {
                        others.Add(new ExpNode_Num(val));
                    }
                    return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(others), true, false);
                }
            }
            else if (node is ExpNode_Plus p)
            {
                BigFraction val = new BigFraction(0, 1);
                List<ExpNode> others = new List<ExpNode>();
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Num num)
                    {
                        val += num.value;
                    }
                    else
                    {
                        others.Add(child);

                    }
                }
                if (others.Count == 0)
                {
                    return new SolverResult<ExpNode>(new ExpNode_Num(val), true, false);
                }
                else if (others.Count == p.nodes.Length || others.Count == p.nodes.Length - 1)
                {
                    return new SolverResult<ExpNode>(node, false, false);
                }
                else
                {
                    //adding zero is a no-op
                    if (!val.IsZero)
                    {
                        others.Add(new ExpNode_Num(val));
                    }
                    return new SolverResult<ExpNode>(ExpNode_Plus.Collapsed(others), true, false);
                }
            }
            return new SolverResult<ExpNode>(node, false, false);
        }
        public bool CanApply(ExpNode node)
        {
            return node is ExpNode_Times || node is ExpNode_Plus || node is ExpNode_Invert || node is ExpNode_Negate;
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
                Dictionary<string, BigFraction> varChildren = new Dictionary<string, BigFraction>();
                Dictionary<string, int> numSources = new Dictionary<string, int>();
                foreach (var child in t.nodes)
                {
                    if (child is ExpNode_Var v)
                    {
                        if (varChildren.ContainsKey(v.name))
                        {
                            varChildren[v.name] += new BigFraction(1, 1);
                            numSources[v.name] += 1;
                        }
                        else
                        {
                            varChildren.Add(v.name, new BigFraction(1, 1));
                            numSources.Add(v.name, 1);
                        }
                    }
                    else if (child is ExpNode_Invert inv)
                    {
                        if (inv.inner is ExpNode_Var innerVar)
                        {
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] -= new BigFraction(1, 1);
                                numSources[innerVar.name] += 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, new BigFraction(-1, 1));
                                numSources.Add(innerVar.name, 1);
                            }
                        }
                        else
                        {
                            plainChildren.Add(child);
                        }
                    }
                    else if (child is ExpNode_Pow innerPow)
                    {
                        if (innerPow.left is ExpNode_Var innerVar && innerPow.right is ExpNode_Num innerNum)
                        {
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] += innerNum.value;
                                numSources[innerVar.name] += 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, innerNum.value);
                                numSources.Add(innerVar.name, 1);
                            }
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
                    if (count.IsZero)
                    {
                        changed = true;
                        //cancel out, nothing to do here
                    }
                    else if (count.IsOne)
                    {
                        //add back variable
                        plainChildren.Add(new ExpNode_Var(v, null));
                    }
                    else if (count == new BigFraction(-1, 1))
                    {
                        //add back 1/variable
                        plainChildren.Add(new ExpNode_Invert(new ExpNode_Var(v, null)));
                    }
                    else
                    {
                        //prevent infinite loop with exponents
                        if (numSources[v] > 1)
                        {
                            changed = true;
                        }
                        //add in v ^ count
                        plainChildren.Add(new ExpNode_Pow(new ExpNode_Var(v, null), new ExpNode_Num(count)));
                    }
                }

                return new SolverResult<ExpNode>(ExpNode_Times.Collapsed(plainChildren), changed, changed);
            }
            else if (node is ExpNode_Plus p)
            {
                List<ExpNode> plainChildren = new List<ExpNode>();
                Dictionary<string, BigFraction> varChildren = new Dictionary<string, BigFraction>();
                Dictionary<string, int> numSources = new Dictionary<string, int>();
                foreach (var child in p.nodes)
                {
                    if (child is ExpNode_Var v)
                    {
                        if (varChildren.ContainsKey(v.name))
                        {
                            varChildren[v.name] += new BigFraction(1, 1);
                            numSources[v.name] += 1;
                        }
                        else
                        {
                            varChildren.Add(v.name, new BigFraction(1, 1));
                            numSources.Add(v.name, 1);
                        }
                    }
                    else if (child is ExpNode_Negate neg)
                    {
                        if (neg.inner is ExpNode_Var innerVar)
                        {
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] -= new BigFraction(1, 1);
                                numSources[innerVar.name] += 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, new BigFraction(-1, 1));
                                numSources.Add(innerVar.name, 1);
                            }
                        }
                        else
                        {
                            plainChildren.Add(child);
                        }
                    }
                    else if (child is ExpNode_Times innerTimes && innerTimes.nodes.Length == 2)
                    {
                        //TODO replace this dumb logic with a recursive scan for var values
                        if (innerTimes.nodes[0] is ExpNode_Num && innerTimes.nodes[1] is ExpNode_Var)
                        {
                            var num = innerTimes.nodes[0] as ExpNode_Num;
                            var innerVar = innerTimes.nodes[1] as ExpNode_Var;
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] += num.value;
                                numSources[innerVar.name] += 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, num.value);
                                numSources.Add(innerVar.name, 1);
                            }
                        }
                        else if (innerTimes.nodes[1] is ExpNode_Num && innerTimes.nodes[0] is ExpNode_Var)
                        {
                            var num = innerTimes.nodes[1] as ExpNode_Num;
                            var innerVar = innerTimes.nodes[0] as ExpNode_Var;
                            if (varChildren.ContainsKey(innerVar.name))
                            {
                                varChildren[innerVar.name] += num.value;
                                numSources[innerVar.name] += 1;
                            }
                            else
                            {
                                varChildren.Add(innerVar.name, num.value);
                                numSources.Add(innerVar.name, 1);
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
                    if (count.IsZero)
                    {
                        changed = true;
                        //cancel out, nothing to do here
                    }
                    else if (count.IsOne)
                    {
                        //add back variable
                        plainChildren.Add(new ExpNode_Var(v, null));
                    }
                    else if (count == new BigFraction(-1, 1))
                    {
                        //add back negated variable
                        plainChildren.Add(new ExpNode_Negate(new ExpNode_Var(v, null)));
                    }
                    else
                    {
                        //prevent infinite loop with multiplication
                        if (numSources[v] > 1)
                        {
                            changed = true;
                        }
                        //add in count * v
                        plainChildren.Add(new ExpNode_Times([new ExpNode_Num(count), new ExpNode_Var(v, null)]));
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