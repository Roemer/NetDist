using System;

namespace NetDist.Core
{
    public class AddJobScriptResult
    {
        public Guid HandlerId { get; set; }
        public AddJobScriptStatus Status { get; set; }
        public AddJobScriptError ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public void SetOk(Guid handlerId, AddJobScriptStatus status)
        {
            HandlerId = handlerId;
            Status = status;
        }

        public void SetError(AddJobScriptError errorCode, string errorMessage)
        {
            Status = AddJobScriptStatus.Error;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }
}
