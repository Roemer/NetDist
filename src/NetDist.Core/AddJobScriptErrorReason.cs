namespace NetDist.Core
{
    public enum AddJobScriptErrorReason
    {
        None,
        ParsingFailed,
        CompilationFailed,
        JobInitializerMissing,
        JobScriptMissing,
        TypeException,
    }
}