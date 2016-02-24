using System;

namespace TMDbLib.Objects.Exceptions
{
    public abstract class TmdbException : Exception
    {
        public TmdbException(string message)
            : base(message)
        {
        }
    }
}