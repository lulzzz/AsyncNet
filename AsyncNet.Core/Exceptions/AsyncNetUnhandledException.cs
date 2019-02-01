using System;

namespace AsyncNet.Core.Exceptions
{
    public class AsyncNetUnhandledException : Exception
    {
        public AsyncNetUnhandledException() : base()
        {
        }

        public AsyncNetUnhandledException(string message) : base(message)
        {
        }

        public AsyncNetUnhandledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
