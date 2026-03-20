//this class might be an unnecessary abstraction, oops
interface TreeNode<Self, Child> : ErrorSource
    where Self : class, TreeNode<Self, Child>
    where Child : class, TreeNode<Child, Child>
{
    //PrettyPrint inherited from ErrorSource
    //public string PrettyPrint();
    public int Precedence();
    public IEnumerable<Child> Children();
    public SolverResult<Self> TransformChildren(Func<Child, bool, SolverResult<Child>> map, bool failEarly);
    public Self CopyRecursive();
}