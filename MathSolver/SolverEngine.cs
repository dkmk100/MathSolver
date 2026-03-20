class VarFinder : TreeScanner<HashSet<string>>
{
    public SolverResult<HashSet<string>> Scan(ExpNode node)
    {
        if (node is ExpNode_Var v)
        {
            return new SolverResult<HashSet<string>>(new HashSet<string>([v.name]));
        }
        return new SolverResult<HashSet<string>>(new HashSet<string>());
    }

    public void Merge(HashSet<string> parent, HashSet<string> child)
    {
        foreach (var item in child)
        {
            parent.Add(item);
        }
    }
}