using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Discover
{
    public enum DiscoverMovieSortBy
    {
        [Obsolete]
        Undefined,
        [Name("popularity.asc")]
        Popularity,
        [Name("popularity.desc")]
        PopularityDesc,
        [Name("release_date.asc")]
        ReleaseDate,
        [Name("release_date.desc")]
        ReleaseDateDesc,
        [Name("revenue.asc")]
        Revenue,
        [Name("revenue.desc")]
        RevenueDesc,
        [Name("primary_release_date.asc")]
        PrimaryReleaseDate,
        [Name("primary_release_date.desc")]
        PrimaryReleaseDateDesc,
        [Name("original_title.asc")]
        OriginalTitle,
        [Name("original_title.desc")]
        OriginalTitleDesc,
        [Name("vote_average.asc")]
        VoteAverage,
        [Name("vote_average.desc")]
        VoteAverageDesc,
        [Name("vote_count.asc")]
        VoteCount,
        [Name("vote_count.desc")]
        VoteCountDesc
    }
}
