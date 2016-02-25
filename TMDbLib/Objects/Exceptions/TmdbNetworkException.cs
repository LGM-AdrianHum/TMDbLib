using System;

namespace TMDbLib.Objects.Exceptions
{
    public class TmdbNetworkException : Exception
    {
        public Exception InnerException { get; }

        public TmdbNetworkException(string message, Exception innerException)
            : base(message)
        {
            InnerException = innerException;
        }
    }
}