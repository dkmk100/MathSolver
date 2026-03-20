class VarFinder : TreeScanner<HashSet<string>>
{
    public SolverResult<HashSet<string>> Scan(ExpNode node)
    {
        //note that scans don't count as transformations

        if (node is ExpNode_Var v)
        {
            return new SolverResult<HashSet<string>>(new HashSet<string>([v.name]), false, false);
        }
        return new SolverResult<HashSet<string>>(new HashSet<string>(), false, false);
    }

    public void Merge(HashSet<string> parent, HashSet<string> child)
    {
        foreach (var item in child)
        {
            parent.Add(item);
        }
    }
}