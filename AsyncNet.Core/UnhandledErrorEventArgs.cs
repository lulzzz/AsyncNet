using System;

namespace AsyncNet.Core
{
    public class UnhandledErrorEventArgs : EventArgs
    {
        public UnhandledErrorEventArgs(UnhandledErrorData unhandledErrorData)
        {
            this.UnhandledErrorData = unhandledErrorData;
        }

        public UnhandledErrorData UnhandledErrorData { get; }
    }
}
