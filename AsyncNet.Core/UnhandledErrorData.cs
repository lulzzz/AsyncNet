using System;

namespace AsyncNet.Core
{
    public class UnhandledErrorData
    {
        public UnhandledErrorData(Exception exception)
        {
            this.Exception = exception;
        }

        public Exception Exception { get; }
    }
}
