using System;

namespace NetDist.Client
{
    public class ClientJobEventArgs : EventArgs
    {
        public ClientJob ClientJob { get; set; }

        public ClientJobEventArgs(ClientJob clientJob)
        {
            ClientJob = clientJob;
        }
    }
}
