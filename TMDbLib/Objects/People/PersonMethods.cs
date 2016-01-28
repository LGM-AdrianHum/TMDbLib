using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.People
{
    [Flags]
    public enum PersonMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("movie_credits")]
        MovieCredits = 1,
        [Name("tv_credits")]
        TvCredits = 2,
        [Name("external_ids")]
        ExternalIds = 4,
        [Name("images")]
        Images = 8,
        [Name("tagged_images")]
        TaggedImages = 16,
        [Name("changes")]
        Changes = 32
    }
}