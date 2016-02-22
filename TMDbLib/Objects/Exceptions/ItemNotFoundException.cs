using System.Net;

namespace TMDbLib.Objects.Exceptions
{
    public class ItemNotFoundException : TmdbHttpException
    {
        public ItemNotFoundException(HttpStatusCode httpStatusCode, TmdbStatusMessage statusMessage)
            : base("The requested item was not found", httpStatusCode, statusMessage)
        {

        }
    }
}