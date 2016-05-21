using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TMDbLib.Objects.Account;
using TMDbLib.Objects.Certifications;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Companies;
using TMDbLib.Objects.Credit;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Genres;
using TMDbLib.Objects.Jobs;
using TMDbLib.Objects.Lists;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.People;
using TMDbLib.Objects.Reviews;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.Timezones;
using TMDbLib.Objects.TvShows;
using TMDbLib.Utilities;
using Credits = TMDbLib.Objects.Movies.Credits;

namespace TMDbChangeDetector
{
    /*
        This tool is meant as a means of finding out if new fields have been added to any TMDb object.

        It works by querying methods and comparing the objects returned with objects stored on disk from previous runs.
        If fields are added or removed, it will be reported.

        Methods are taken from docs:
            http://docs.themoviedb.apiary.io/
    */
    class Program
    {
        private const int IdAvatar = 19995;
        private const int IdBreakingBad = 1396;
        private const string IdBruceWillisMiamiVice = "525719bb760ee3776a1835d3";
        private const int IdBruceWillis = 62;
        private const int IdTwentiethCenturyFox = 25;
        private const int IdHbo = 49;
        private const int IdJamesBondCollection = 645;
        private const int IdGenreAction = 28;
        private const int IdKeywordRogue = 186447;
        private const string IdTheDarkKnightRisesReviewId = "5010553819c2952d1b000451";
        private const string ImdbBreakingBadId = "tt0903747";
        private const string TestListId = "528349d419c2954bd21ca0a8";
        private const int IdBreakingBadSeason1 = 3572;
        private const int IdBreakingBadSeason1Episode1 = 62085;

        public const string ApiKey = "c6b31d1cdad6a56a23f0c913e2482a31";
        private const string SessionId = "c413282cdadad9af972c06d9b13096a8b13ab1c1";
        private const string GuestTestSessionId = "d425468da2781d6799ba14c05f7327e7";
        private const string AccountId = "6089455";

        public static readonly Uri BaseUri = new Uri("https://api.themoviedb.org/3/");

        static void Main(string[] args)
        {
            List<RequestDescriptor> xx = SetupMethods().ToList();

            foreach (RequestDescriptor descriptor in xx)
            {
                ProcessDescriptor(descriptor);

                Console.WriteLine();
                Console.ReadLine();
            }
        }

        static void ProcessDescriptor(RequestDescriptor descriptor)
        {
            Console.Write("Processing ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{descriptor.Method} {descriptor.Path} ");
            Console.ResetColor();

            if (descriptor.TmdbLibType != null)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"(Type: {Helpers.PrettyPrintType(descriptor.TmdbLibType)})");
                Console.ResetColor();
            }

            Console.WriteLine();

            // Get current
            string current;
            try
            {
                current = Helpers.GetJson(descriptor);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.ResetColor();
                return;
            }

            if (descriptor.TmdbLibType == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("MISSING TYPE. Can't compare");

                Console.WriteLine("JSON response:");
                Console.WriteLine(JToken.Parse(current).ToString(Formatting.Indented));

                Console.WriteLine();

                Console.ResetColor();
            }
            else
            {
                // Get differences to current
                TmdbDifferences diff = CalculateDiff(descriptor, current);

                if (diff.IsSame)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("No differences");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"DIFFERENT. Missing: {diff.NewFields.Count:N0}, Excess: {diff.MissingFields.Count:N0}");
                    Console.WriteLine();

                    if (diff.NewFields.Any())
                    {
                        Console.WriteLine("New fields (present in JSON)");
                        foreach (var key in diff.NewFields)
                            Console.WriteLine($"  {key.Key} - {key.Value.ErrorContext.Error.Message}");

                        Console.WriteLine();
                    }

                    if (diff.MissingFields.Any())
                    {
                        Console.WriteLine("Missing fields (present in C#)");
                        foreach (var key in diff.MissingFields)
                            Console.WriteLine($"  {key.Key} - {key.Value.ErrorContext.Error.Message}");

                        Console.WriteLine();
                    }
                }
            }
        }

        static TmdbDifferences CalculateDiff(RequestDescriptor descriptor, string current)
        {
            TmdbDifferences result = new TmdbDifferences();
            result.Request = descriptor;

            // Deserialize
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.ContractResolver = new ContractResolver();
            settings.MissingMemberHandling = MissingMemberHandling.Error;

            List<ErrorEventArgs> errors = new List<ErrorEventArgs>();
            settings.Error += (sender, args) =>
            {
                errors.Add(args);
                args.ErrorContext.Handled = true;
            };

            try
            {
                JsonConvert.DeserializeObject(current, descriptor.TmdbLibType, settings);
            }
            catch (JsonSerializationException)
            {
            }

            // Parse errors
            foreach (ErrorEventArgs error in errors)
            {
                // Required property 'items' not found in JSON. Path 'results[0]', line 1, position 199.
                // Could not find member 'rating' on object of type 'SearchMovie'. Path 'results[0].rating', line 1, position 541

                var key = error.ErrorContext.Path + "/" + error.ErrorContext.Member;
                var errorMessage = error.ErrorContext.Error.Message;

                key = Helpers.NormalizeErrorKey(key);

                if (errorMessage.StartsWith("Required property"))
                {
                    // Field in C# is missing in JSON
                    if (!result.MissingFields.ContainsKey(key))
                        result.MissingFields.Add(key, error);
                }
                else if (errorMessage.StartsWith("Could not find member"))
                {
                    // Field in JSON is missing in C#
                    if (!result.NewFields.ContainsKey(key))
                        result.NewFields.Add(key, error);
                }
                else
                {
                    throw new Exception("Unknown error type");
                }
            }

            return result;
        }

        static IEnumerable<RequestDescriptor> SetupMethods()
        {
            // Configuration
            yield return new RequestDescriptor<TMDbConfig>("Configuration", "/configuration");

            // Account
            yield return new RequestDescriptor<AccountDetails>("Account", "/account", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<List>>("Account", $"/account/{AccountId}/lists", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchMovie>>("Account", $"/account/{AccountId}/favorite/movies", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("Account", $"/account/{AccountId}/favorite/tv", new[] { Helpers.Create("session_id", SessionId) });
            // TODO: POST yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchMovie>>("Account", $"/account/{AccountId}/rated/movies", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("Account", $"/account/{AccountId}/rated/tv", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchTvEpisode>>("Account", $"/account/{AccountId}/rated/tv/episodes", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchMovie>>("Account", $"/account/{AccountId}/watchlist/movies", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("Account", $"/account/{AccountId}/watchlist/tv", new[] { Helpers.Create("session_id", SessionId) });
            // TODO: POST  yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist", new[] { Create("session_id", SessionId) });

            // Authentication
            // /authentication/token/new
            // /authentication/token/validate_with_login
            // /authentication/session/new
            // /authentication/guest_session/new

            // Certifications
            yield return new RequestDescriptor<CertificationsContainer>("Certifications", "/certification/movie/list");
            yield return new RequestDescriptor<CertificationsContainer>("Certifications", "/certification/tv/list");

            // Changes
            yield return new RequestDescriptor<SearchContainer<ChangesListItem>>("Changes", "/movie/changes");
            yield return new RequestDescriptor<SearchContainer<ChangesListItem>>("Changes", "/person/changes");
            yield return new RequestDescriptor<SearchContainer<ChangesListItem>>("Changes", "/tv/changes");

            // Collections
            yield return new RequestDescriptor<Collection>("Collections", $"/collection/{IdJamesBondCollection}");
            yield return new RequestDescriptor<ImagesWithId>("Collections", $"/collection/{IdJamesBondCollection}/images");

            // Companies
            yield return new RequestDescriptor<Company>("Companies", $"/company/{IdTwentiethCenturyFox}");
            yield return new RequestDescriptor<SearchContainerWithId<MovieResult>>("Companies", $"/company/{IdTwentiethCenturyFox}/movies");

            // Credits
            yield return new RequestDescriptor<Credit>("Credits", $"/credit/{IdBruceWillisMiamiVice}");

            // Discover
            yield return new RequestDescriptor<SearchContainer<SearchMovie>>("Discover", "/discover/movie");
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("Discover", "/discover/tv");

            // Find
            yield return new RequestDescriptor<FindContainer>("Find", $"/find/{ImdbBreakingBadId}", new[] { Helpers.Create("external_source", FindExternalSource.Imdb.GetDescription()) });

            // Genres
            yield return new RequestDescriptor<GenreContainer>("Genres", "/genre/movie/list");
            yield return new RequestDescriptor<GenreContainer>("Genres", "/genre/tv/list");
            yield return new RequestDescriptor<SearchContainerWithId<MovieResult>>("Genres", $"/genre/{IdGenreAction}/movies");

            // Guest Sessions
            yield return new RequestDescriptor<SearchContainer<MovieWithRating>>("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/movies");
            yield return new RequestDescriptor<SearchContainer<TvShowWithRating>>("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/tv");
            yield return new RequestDescriptor<SearchContainer<TvEpisodeWithRating>>("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/tv/episodes");

            // Jobs
            yield return new RequestDescriptor<JobContainer>("Jobs", "/job/list");

            // Keywords
            yield return new RequestDescriptor<Keyword>("Keywords", $"/keyword/{IdKeywordRogue}");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Keywords", $"/keyword/{IdKeywordRogue}/movies");

            // Lists
            yield return new RequestDescriptor<List>("Lists", $"/list/{TestListId}");
            yield return new RequestDescriptor<ListStatus>("Lists", $"/list/{TestListId}/item_status", new[] { Helpers.Create("movie_id", IdAvatar.ToString()) });

            // TODO: /list
            // TODO: /list/id/add_item
            // TODO: /list/id/remove_item
            // TODO: /list/id/clear

            // Movies
            yield return new RequestDescriptor<Movie>("Movies", $"/movie/{IdAvatar}");
            TODO: yield return new RequestDescriptor<AccountState>("Movies", $"/movie/{IdAvatar}/account_states", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<AlternativeTitles>("Movies", $"/movie/{IdAvatar}/alternative_titles");
            yield return new RequestDescriptor<Credits>("Movies", $"/movie/{IdAvatar}/credits");
            yield return new RequestDescriptor<Images>("Movies", $"/movie/{IdAvatar}/images");
            yield return new RequestDescriptor<KeywordsContainer>("Movies", $"/movie/{IdAvatar}/keywords");
            yield return new RequestDescriptor<Releases>("Movies", $"/movie/{IdAvatar}/release_dates");
            yield return new RequestDescriptor<ResultContainer<Video>>("Movies", $"/movie/{IdAvatar}/videos");
            yield return new RequestDescriptor<TranslationsContainer>("Movies", $"/movie/{IdAvatar}/translations");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Movies", $"/movie/{IdAvatar}/similar");
            yield return new RequestDescriptor<SearchContainer<Review>>("Movies", $"/movie/{IdAvatar}/reviews");
            yield return new RequestDescriptor<SearchContainer<ListResult>>("Movies", $"/movie/{IdAvatar}/lists");
            yield return new RequestDescriptor<ChangesContainer>("Movies", $"/movie/{IdAvatar}/changes");
            // TODO: yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/rating", tmdbLibType: typeof(object));
            yield return new RequestDescriptor<Movie>("Movies", "/movie/latest");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Movies", "/movie/now_playing");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Movies", "/movie/popular");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Movies", "/movie/top_rated");
            yield return new RequestDescriptor<SearchContainer<MovieResult>>("Movies", "/movie/upcoming");

            // Networks
            yield return new RequestDescriptor<Network>("Networks", $"/network/{IdTwentiethCenturyFox}");

            // People
            yield return new RequestDescriptor<Person>("People", $"/person/{IdBruceWillis}");
            yield return new RequestDescriptor<MovieCredits>("People", $"/person/{IdBruceWillis}/movie_credits");
            yield return new RequestDescriptor<TvCredits>("People", $"/person/{IdBruceWillis}/tv_credits");
            yield return new RequestDescriptor<object>("People", $"/person/{IdBruceWillis}/combined_credits");
            yield return new RequestDescriptor<ExternalIds>("People", $"/person/{IdBruceWillis}/external_ids");
            yield return new RequestDescriptor<ProfileImages>("People", $"/person/{IdBruceWillis}/images");
            yield return new RequestDescriptor<SearchContainer<TaggedImage>>("People", $"/person/{IdBruceWillis}/tagged_images");
            yield return new RequestDescriptor<ChangesContainer>("People", $"/person/{IdBruceWillis}/changes");
            yield return new RequestDescriptor<SearchContainer<PersonResult>>("People", "/person/popular");
            yield return new RequestDescriptor<Person>("People", "/person/latest");

            // Reviews
            yield return new RequestDescriptor<Review>("Reviews", $"/review/{IdTheDarkKnightRisesReviewId}");

            // Search
            yield return new RequestDescriptor<SearchContainer<SearchCompany>>("Search", "/search/company", new[] { Helpers.Create("query", "hbo") });
            yield return new RequestDescriptor<SearchContainer<SearchResultCollection>>("Search", "/search/collection", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor<SearchContainer<SearchKeyword>>("Search", "/search/keyword", new[] { Helpers.Create("query", "tower") });
            yield return new RequestDescriptor<SearchContainer<SearchList>>("Search", "/search/list", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor<SearchContainer<SearchMovie>>("Search", "/search/movie", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor<SearchContainer<SearchMulti>>("Search", "/search/multi", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor<SearchContainer<SearchPerson>>("Search", "/search/person", new[] { Helpers.Create("query", "bruce") });
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("Search", "/search/tv", new[] { Helpers.Create("query", "house") });

            // Timezones
            yield return new RequestDescriptor<Timezones>("Timezones", "/timezones/list");

            // TV
            yield return new RequestDescriptor<TvShow>("TV", $"/tv/{IdBreakingBad}");
            yield return new RequestDescriptor<AccountState>("TV", $"/tv/{IdBreakingBad}/account_states", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<ResultContainer<AlternativeTitle>>("TV", $"/tv/{IdBreakingBad}/alternative_titles");
            yield return new RequestDescriptor<ChangesContainer>("TV", $"/tv/{IdBreakingBad}/changes");
            yield return new RequestDescriptor<ResultContainer<ContentRating>>("TV", $"/tv/{IdBreakingBad}/content_ratings");
            yield return new RequestDescriptor<Credits>("TV", $"/tv/{IdBreakingBad}/credits");
            yield return new RequestDescriptor<ExternalIds>("TV", $"/tv/{IdBreakingBad}/external_ids");
            yield return new RequestDescriptor<ImagesWithId>("TV", $"/tv/{IdBreakingBad}/images");
            yield return new RequestDescriptor<ResultContainer<Keyword>>("TV", $"/tv/{IdBreakingBad}/keywords");
            yield return new RequestDescriptor<ResultContainer<ContentRating>>("TV", $"/tv/{IdBreakingBad}/rating");
            yield return new RequestDescriptor<SearchContainer<SearchTv>>("TV", $"/tv/{IdBreakingBad}/similar");
            yield return new RequestDescriptor<TranslationsContainer>("TV", $"/tv/{IdBreakingBad}/translations");
            yield return new RequestDescriptor<ResultContainer<Video>>("TV", $"/tv/{IdBreakingBad}/videos");
            yield return new RequestDescriptor<TvShow>("TV", "/tv/latest");
            yield return new RequestDescriptor<SearchContainer<TvShow>>("TV", "/tv/on_the_air");
            yield return new RequestDescriptor<SearchContainer<TvShow>>("TV", "/tv/airing_today");
            yield return new RequestDescriptor<SearchContainer<TvShow>>("TV", "/tv/top_rated");
            yield return new RequestDescriptor<SearchContainer<TvShow>>("TV", "/tv/popular");

            // TV Seasons
            yield return new RequestDescriptor<TvSeason>("TV Seasons", $"/tv/{IdBreakingBad}/season/1");
            yield return new RequestDescriptor<ChangesContainer>("TV Seasons", $"/tv/season/{IdBreakingBadSeason1}/changes");
            yield return new RequestDescriptor<ResultContainer<TvEpisodeAccountState>>("TV Seasons", $"/tv/{IdBreakingBad}/season/1/account_states", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<Credits>("TV Seasons", $"/tv/{IdBreakingBad}/season/1/credits");
            yield return new RequestDescriptor<ExternalIds>("TV Seasons", $"/tv/{IdBreakingBad}/season/1/external_ids");
            yield return new RequestDescriptor<PosterImages>("TV Seasons", $"/tv/{IdBreakingBad}/season/1/images");
            yield return new RequestDescriptor<ResultContainer<Video>>("TV Seasons", $"/tv/{IdBreakingBad}/season/1/videos");

            // TV Episodes
            yield return new RequestDescriptor<TvEpisode>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1");
            yield return new RequestDescriptor<ChangesContainer>("TV Episodes", $"/tv/episode/{IdBreakingBadSeason1Episode1}/changes");
            yield return new RequestDescriptor<TvEpisodeAccountState>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/account_states", new[] { Helpers.Create("session_id", SessionId) });
            yield return new RequestDescriptor<Credits>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/credits");
            yield return new RequestDescriptor<ExternalIds>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/external_ids");
            yield return new RequestDescriptor<StillImages>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/images");
            // TODO: yield return new RequestDescriptor<TvShow>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/rating");
            yield return new RequestDescriptor<ResultContainer<Video>>("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/videos");
        }
    }
}
