
interface TreeScanner<T> where T : class
{
    public SolverResult<T> Scan(ExpNode node);
    public void Merge(T parent, T child);//works because T is a reference type
    public SolverResult<Dictionary<ExpNode, T>> ScanRecursive(ExpNode node, bool failEarly, Dictionary<ExpNode, T> dict = null)
    {
        if (dict == null)
        {
            dict = new Dictionary<ExpNode, T>();
        }
        SolverResult<Dictionary<ExpNode, T>> result = new SolverResult<Dictionary<ExpNode, T>>(dict, false, false);
        Stack<(ExpNode, T?, T?)> stack = new Stack<(ExpNode, T?, T?)>();//works because T is a reference type
        stack.Push((node, null, null));
        while (stack.Count() > 0)
        {
            (ExpNode target, T? parent, T? self) = stack.Pop();
            if (self == null)
            {
                //this is the initial visit
                SolverResult<T> temp = Scan(target);
                result.MergeChildStatus(temp);
                if (temp.Success())
                {
                    //push visited version to stack
                    stack.Push((target, parent, temp.result!));
                    dict.Add(target, temp.result!);
                    foreach (var child in target.Children().Reverse())
                    {
                        stack.Push((child, temp.result!, null));
                    }
                }
                else if (failEarly)
                {
                    return result;
                }
                else
                {
                    foreach (var child in target.Children().Reverse())
                    {
                        stack.Push((child, null, null));
                    }
                }
            }
            else
            {
                //cleanup pass, already scanned

                //merge with parent
                if (parent != null)
                {
                    Merge(parent, self);
                }
            }
        }
        return result;
    }
}