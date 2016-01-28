using TMDbLib.Utilities;

namespace TMDbLib.Objects.Movies
{
    public enum TvShowListType
    {
        [Name("on_the_air")]
        OnTheAir,
        [Name("airing_today")]
        AiringToday,
        [Name("top_rated")]
        TopRated,
        [Name("popular")]
        Popular
    }
}