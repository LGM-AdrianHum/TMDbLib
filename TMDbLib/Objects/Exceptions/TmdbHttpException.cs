using System.Net;

namespace TMDbLib.Objects.Exceptions
{
    public abstract class TmdbHttpException : TmdbException
    {
        public HttpStatusCode HttpStatusCode { get; }

        public TmdbHttpException(string message, HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base(message, statusMessage)
        {
            HttpStatusCode = httpStatusCode;
        }
    }
}