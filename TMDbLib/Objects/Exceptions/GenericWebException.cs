using System.Net;

namespace TMDbLib.Objects.Exceptions
{
    public class GenericWebException : TmdbHttpException
    {
        public GenericWebException(HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base("I have no idea", httpStatusCode, statusMessage)
        {

        }
    }
}