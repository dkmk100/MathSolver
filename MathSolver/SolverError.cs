class SolverError
{
    public enum ErrorType { ParseError, MathError }

    ErrorType type;
    string msg;
    ExpNode? source;
    public SolverError(ErrorType type, string msg, ExpNode? source)
    {
        this.type = type;
        this.msg = msg;
        this.source = source;
    }

    public string PrettyPrint()
    {
        return type + ": " + msg + (source == null ? "" : " at node: {" + source.PrettyPrint() + "}");
    }
}