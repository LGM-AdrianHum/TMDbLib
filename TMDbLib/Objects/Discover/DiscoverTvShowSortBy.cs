using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Discover
{
    public enum DiscoverTvShowSortBy
    {
        [Obsolete]
        Undefined,
        [Name("vote_average.asc")]
        VoteAverage,
        [Name("vote_average.desc")]
        VoteAverageDesc,
        [Name("first_air_date.asc")]
        FirstAirDate,
        [Name("first_air_date.desc")]
        FirstAirDateDesc,
        [Name("popularity.asc")]
        Popularity,
        [Name("popularity.desc")]
        PopularityDesc
    }
}
