struct SolverResult<T> where T : class
{
    public T? result { get; private set; }
    public bool transformed;
    public bool transformedChildren;
    public readonly List<SolverError> errors;
    public SolverResult(T val, bool transformed, bool transformedChildren)
    {
        this.result = val;
        this.transformed = transformed;
        this.transformedChildren = transformedChildren;
        if (transformed || transformedChildren)
        {

        }
        this.errors = new List<SolverError>();
    }
    public SolverResult(SolverError error)
    {
        this.result = null;
        this.errors = new List<SolverError>();
        errors.Add(error);
    }
    public SolverResult(params SolverError[] errors)
    {
        this.result = null;
        this.errors = new List<SolverError>();
        this.errors.AddRange(errors);
    }
    public bool Success()
    {
        return result != null;
    }
    public SolverResult<T> MergePeerStatus<Q>(SolverResult<Q> other) where Q : class
    {
        //propagate error status
        errors.AddRange(other.errors);
        if (other.result == null)
        {
            result = null;
        }

        //also propagate transformation status
        if (other.transformed)
        {
            this.transformed = true;
        }
        if (other.transformedChildren)
        {
            this.transformedChildren = true;
        }
        return this;
    }
    public SolverResult<T> MergeChildStatus<Q>(SolverResult<Q> other) where Q : class
    {
        //propagate error status
        errors.AddRange(other.errors);
        if (other.result == null)
        {
            result = null;
        }

        //also propagate transformation status
        if (other.transformed || other.transformedChildren)
        {
            this.transformedChildren = true;
        }

        return this;
    }
}