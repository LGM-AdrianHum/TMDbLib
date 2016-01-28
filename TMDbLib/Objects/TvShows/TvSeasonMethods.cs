using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.TvShows
{
    [Flags]
    public enum TvSeasonMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("credits")]
        Credits = 1,
        [Name("images")]
        Images = 2,
        [Name("external_ids")]
        ExternalIds = 4,
        [Name("videos")]
        Videos = 8,
        [Name("account_states")]
        AccountStates = 16,
    }
}
