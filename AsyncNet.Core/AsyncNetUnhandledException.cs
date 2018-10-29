using System;

namespace AsyncNet.Core
{
    public class AsyncNetUnhandledException : Exception
    {
        public AsyncNetUnhandledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
