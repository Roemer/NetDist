namespace NetDist.Core
{
    public enum AddJobScriptErrorReason
    {
        None,
        ParsingFailed,
        CompilationFailed,
        HandlerInitializerMissing,
        JobScriptMissing,
        TypeException,
    }
}