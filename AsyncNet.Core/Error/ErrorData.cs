using System;

namespace AsyncNet.Core.Error
{
    public class ErrorData
    {
        public ErrorData(Exception exception)
        {
            this.Exception = exception;
        }

        public Exception Exception { get; }
    }
}
