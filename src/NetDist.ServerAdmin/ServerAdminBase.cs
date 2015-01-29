using NetDist.Core;
using System;

namespace NetDist.ServerAdmin
{
    public abstract class ServerAdminBase
    {
        public abstract ServerInfo GetStatistics();
        public abstract PackageInfo GetPackages();
        public abstract void AddPackage(PackageInfo packageInfo, byte[] packageZip);
        public abstract AddJobScriptResult AddJobScript(JobScriptInfo jobScriptInfo);
        public abstract void RemoveJobScript(Guid handlerId);
        public abstract void StartJobScript(Guid handlerId);
        public abstract void StopJobScript(Guid handlerId);
        public abstract void PauseJobScript(Guid handlerId);
        public abstract void EnableJobScript(Guid handlerId);
        public abstract void DisableJobScript(Guid handlerId);
    }
}
