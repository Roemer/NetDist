using System;
using NetDist.Core;

namespace NetDist.Server.XDomainObjects
{
    [Serializable]
    public class JobScriptInitializeResult
    {
        public bool HasError { get; set; }
        public AddJobScriptErrorReason ErrorReason { get; set; }
        public string CompileOutput { get; set; }
        public string ErrorMessage { get; set; }
        public Guid HandlerId { get; set; }
        public string PackageName { get; set; }
        public string HandlerName { get; set; }
        public string JobName { get; set; }

        public void SetError(AddJobScriptErrorReason errorReason, string errorMessage)
        {
            HasError = true;
            ErrorMessage = errorMessage;
            ErrorReason = errorReason;
        }
    }
}
