namespace NetDist.Core
{
    public enum AddJobHandlerErrorReason
    {
        None,
        ParsingFailed,
        CompilationFailed,
        JobInitializerMissing,
        JobHandlerMissing
    }
}