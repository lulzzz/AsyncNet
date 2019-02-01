using System;

namespace AsyncNet.Core.Error.SystemEvent
{
    public class UnhandledErrorEventArgs : EventArgs
    {
        public UnhandledErrorEventArgs(ErrorData errorData)
        {
            this.ErrorData = errorData;
        }

        public ErrorData ErrorData { get; }
    }
}
