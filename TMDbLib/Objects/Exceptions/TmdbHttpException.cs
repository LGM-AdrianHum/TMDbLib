using System.Net;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Exceptions
{
    public abstract class TmdbHttpException : TmdbException
    {
        public HttpStatusCode HttpStatusCode { get; }

        public TmdbStatusMessage StatusMessage { get; }

        public TmdbHttpException(string message, HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
            StatusMessage = statusMessage;
        }
    }
}