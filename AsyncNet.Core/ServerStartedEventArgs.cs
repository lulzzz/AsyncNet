using System;

namespace AsyncNet.Core
{
    public class ServerStartedEventArgs : EventArgs
    {
        public ServerStartedEventArgs(ServerStartedData serverStartedData)
        {
            this.ServerStartedData = serverStartedData;
        }

        public ServerStartedData ServerStartedData { get; }
    }
}
