using NetDist.Core;
using System;

namespace NetDist.Server.XDomainObjects
{
    [Serializable]
    public class JobScriptInitializeResult
    {
        public Guid HandlerId { get; set; }
        public bool HasError { get; set; }
        public AddJobScriptError ErrorReason { get; set; }
        public string CompileOutput { get; set; }
        public string ErrorMessage { get; set; }

        public void SetError(AddJobScriptError errorReason, string errorMessage)
        {
            HasError = true;
            ErrorMessage = errorMessage;
            ErrorReason = errorReason;
        }
    }
}
