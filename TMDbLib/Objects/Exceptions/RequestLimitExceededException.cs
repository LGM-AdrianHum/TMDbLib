using System.Net;

namespace TMDbLib.Objects.Exceptions
{
    public class RequestLimitExceededException : TmdbHttpException
    {
        public RequestLimitExceededException(HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base("You have exceeded the maximum number of request allowed by TMDb please try again later", httpStatusCode, statusMessage)
        {

        }
    }
}