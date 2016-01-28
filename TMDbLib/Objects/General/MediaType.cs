using TMDbLib.Utilities;

namespace TMDbLib.Objects.General
{
    public enum MediaType
    {
        Unknown,

        [Name("movie")]
        Movie,

        [Name("tv")]
        TVShow
    }
}