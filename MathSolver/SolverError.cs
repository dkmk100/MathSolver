class SolverError
{
    public enum ErrorType { InternalError, ParseError, MathError, SolveError }

    ErrorType type;
    string msg;
    ErrorSource? source;
    public SolverError(ErrorType type, string msg, ErrorSource? source)
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

interface ErrorSource
{
    public string PrettyPrint();
    //TODO track actually useful info here, eg. source token range or something
}