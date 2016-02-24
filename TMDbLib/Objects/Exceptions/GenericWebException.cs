using System.Net;
using TMDbLib.Objects.General;

namespace TMDbLib.Objects.Exceptions
{
    public class GenericWebException : TmdbHttpException
    {
        public GenericWebException(HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base("A generic HTTP error happened", httpStatusCode, statusMessage)
        {

        }
    }
}