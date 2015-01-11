
namespace NetDist.Server
{
    public class JobLogicFile
    {
        public bool ParsingFailed { get; private set; }
        public string ErrorMessage { get; private set; }
        public string HandlerSettingsString { get; set; }
        public string HandlerCustomSettingsString { get; set; }
        public string CompilerSettings { get; set; }
        public string JobLogic { get; set; }
        public string ExampleInput { get; set; }

        public void SetError(string errorMessage)
        {
            ParsingFailed = true;
            ErrorMessage = errorMessage;
        }
    }
}
