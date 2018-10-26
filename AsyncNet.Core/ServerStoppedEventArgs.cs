using System;

namespace AsyncNet.Core
{
    public class ServerStoppedEventArgs : EventArgs
    {
        public ServerStoppedEventArgs(ServerStoppedData serverStoppedData)
        {
            this.ServerStoppedData = serverStoppedData;
        }

        public ServerStoppedData ServerStoppedData { get; }
    }
}
