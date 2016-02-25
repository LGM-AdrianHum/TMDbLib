using System.Net;

namespace TMDbLib.Objects.Exceptions
{
    public class BadResponseTypeException : TmdbHttpException
    {
        public BadResponseTypeException(HttpStatusCode httpStatusCode)
            : base("The HTTP response Content-Type was not a JSON format", httpStatusCode, null)
        {

        }
    }
}