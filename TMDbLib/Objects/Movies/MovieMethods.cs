using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Movies
{
    [Flags]
    public enum MovieMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("alternative_titles")]
        AlternativeTitles = 1,
        [Name("credits")]
        Credits = 2,
        [Name("images")]
        Images = 4,
        [Name("keywords")]
        Keywords = 8,
        [Name("releases")]
        Releases = 16,
        [Name("videos")]
        Videos = 32,
        [Name("translations")]
        Translations = 64,
        [Name("similar")]
        Similar = 128,
        [Name("reviews")]
        Reviews = 256,
        [Name("lists")]
        Lists = 512,
        [Name("changes")]
        Changes = 1024,
        /// <summary>
        /// Requires a valid user session to be set on the client object
        /// </summary>
        [Name("account_states")]
        AccountStates = 2048,
        [Name("release_dates")]
        ReleaseDates = 4096
    }
}