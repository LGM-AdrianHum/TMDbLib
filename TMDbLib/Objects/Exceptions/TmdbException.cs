using System;

namespace TMDbLib.Objects.Exceptions
{
    public abstract class TmdbException : Exception
    {
        public TmdbStatusMessage StatusMessage { get; }

        public TmdbException(string message, TmdbStatusMessage statusMessage)
            : base(message)
        {
            StatusMessage = statusMessage;
        }
    }
}