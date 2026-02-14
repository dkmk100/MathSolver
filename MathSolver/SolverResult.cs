struct SolverResult
{
    public ExpNode? result { get; private set; }
    public readonly List<SolverError> errors;
    public SolverResult(ExpNode val)
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
    public void MergeErrors(SolverResult other)
    {
        errors.AddRange(other.errors);
        if (other.result == null)
        {
            result = null;
        }
    }
}