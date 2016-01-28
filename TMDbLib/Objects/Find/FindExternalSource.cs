using TMDbLib.Utilities;

namespace TMDbLib.Objects.Find
{
    public enum FindExternalSource
    {
        [Name("imdb_id")]
        Imdb,
        [Name("freebase_mid")]
        FreeBaseMid,
        [Name("freebase_id")]
        FreeBaseId,
        [Name("tvrage_id")]
        TvRage,
        [Name("tvdb_id")]
        TvDb
    }
}