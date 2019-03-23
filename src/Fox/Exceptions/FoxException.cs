using System;

namespace Fox.Exceptions
{
    public sealed class FoxException : Exception
    {
        public FoxException(string message) : base(message) { }
        public FoxException(string message, Exception innerException) : base(message, innerException) { }
    }
}
