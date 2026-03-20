struct SolverResult<T> where T : class
{
    public T? result { get; private set; }
    public readonly List<SolverError> errors;
    public SolverResult(T val)
    {
        this.result = val;
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
    public SolverResult<T> MergeErrors<Q>(SolverResult<Q> other) where Q : class
    {
        errors.AddRange(other.errors);
        if (other.result == null)
        {
            result = null;
        }
        return this;
    }
}