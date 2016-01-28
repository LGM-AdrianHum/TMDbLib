using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.TvShows
{
    [Flags]
    public enum TvShowMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("credits")]
        Credits = 1,
        [Name("images")]
        Images = 2,
        [Name("external_ids")]
        ExternalIds = 4,
        [Name("content_ratings")]
        ContentRatings = 8,
        [Name("alternative_titles")]
        AlternativeTitles = 16,
        [Name("keywords")]
        Keywords = 32,
        [Name("similar")]
        Similar = 64,
        [Name("videos")]
        Videos = 128,
        [Name("translations")]
        Translations = 256,
        [Name("account_states")]
        AccountStates = 512,
        [Name("changes")]
        Changes = 1024
    }
}
