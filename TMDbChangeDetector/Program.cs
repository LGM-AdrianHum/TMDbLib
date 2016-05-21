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
using TMDbLib.Objects.General;
using TMDbLib.Objects.Genres;
using TMDbLib.Objects.Jobs;
using TMDbLib.Objects.Lists;
using TMDbLib.Objects.Search;

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

        public const string ApiKey = "c6b31d1cdad6a56a23f0c913e2482a31";
        private const string SessionId = "c413282cdadad9af972c06d9b13096a8b13ab1c1";
        private const string AccountId = "6089455";

        public static readonly Uri BaseUri = new Uri("https://api.themoviedb.org/3/");

        static void Main(string[] args)
        {
            List<RequestDescriptor> xx = SetupMethods().ToList();

            foreach (RequestDescriptor descriptor in xx)
            {
                ProcessDescriptor(descriptor);

                Console.WriteLine();
                //Console.ReadLine();
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
            yield return new RequestDescriptor("Configuration", "/configuration", tmdbLibType: typeof(TMDbConfig));

            // Account
            yield return new RequestDescriptor("Account", "/account", new[] { Helpers.Create("session_id", SessionId) }, typeof(AccountDetails));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/lists", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<List>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite/movies", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite/tv", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchTv>));
            // TODO: POST yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/movies", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/tv", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchTv>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/tv/episodes", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchTvEpisode>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist/movies", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist/tv", new[] { Helpers.Create("session_id", SessionId) }, typeof(SearchContainer<SearchTv>));
            // TODO: POST  yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist", new[] { Create("session_id", SessionId) });

            // Authentication
            // /authentication/token/new
            // /authentication/token/validate_with_login
            // /authentication/session/new
            // /authentication/guest_session/new

            // Certifications
            yield return new RequestDescriptor("Certifications", "/certification/movie/list", tmdbLibType: typeof(CertificationsContainer));
            yield return new RequestDescriptor("Certifications", "/certification/tv/list", tmdbLibType: typeof(CertificationsContainer));

            // Changes
            yield return new RequestDescriptor("Changes", "/movie/changes", tmdbLibType: typeof(SearchContainer<ChangesListItem>));
            yield return new RequestDescriptor("Changes", "/person/changes", tmdbLibType: typeof(SearchContainer<ChangesListItem>));
            yield return new RequestDescriptor("Changes", "/tv/changes", tmdbLibType: typeof(SearchContainer<ChangesListItem>));

            // Collections
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}", tmdbLibType: typeof(Collection));
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}/images", tmdbLibType: typeof(ImagesWithId));

            // Companies
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}", tmdbLibType: typeof(Company));
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}/movies", tmdbLibType: typeof(SearchContainerWithId<MovieResult>));

            // Credits
            yield return new RequestDescriptor("Credits", $"/credit/{IdBruceWillisMiamiVice}", tmdbLibType: typeof(Credit));

            // Discover
            yield return new RequestDescriptor("Discover", "/discover/movie", tmdbLibType: typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Discover", "/discover/tv", tmdbLibType: typeof(SearchContainer<SearchTv>));

            // Find
            // /find/id

            // Genres
            yield return new RequestDescriptor("Genres", "/genre/movie/list", tmdbLibType: typeof(GenreContainer));
            yield return new RequestDescriptor("Genres", "/genre/tv/list", tmdbLibType: typeof(GenreContainer));
            yield return new RequestDescriptor("Genres", $"/genre/{IdGenreAction}/movies", tmdbLibType: typeof(SearchContainerWithId<MovieResult>));

            // Guest Sessions
            // /guest_session/guest_session_id/rated/movies
            // /guest_session/guest_session_id/rated/tv
            // /guest_session/guest_session_id/rated/tv/episodes

            // Jobs
            yield return new RequestDescriptor("Jobs", "/job/list", tmdbLibType: typeof(JobContainer));

            // Keywords
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}", tmdbLibType: typeof(Keyword));
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}/movies", tmdbLibType: typeof(SearchContainer<MovieResult>));

            // Lists
            // /list/id
            // /list/id/item_status
            // /list
            // /list/id/add_item
            // /list/id/remove_item
            // /list/id/clear

            // Movies
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}");
            // TODO: yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/account_states");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/alternative_titles");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/credits");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/images");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/keywords");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/release_dates");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/videos");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/translations");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/similar");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/reviews");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/lists");
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/changes");
            // TODO: yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/rating");
            yield return new RequestDescriptor("Movies", "/movie/latest");
            yield return new RequestDescriptor("Movies", "/movie/now_playing");
            yield return new RequestDescriptor("Movies", "/movie/popular");
            yield return new RequestDescriptor("Movies", "/movie/top_rated");
            yield return new RequestDescriptor("Movies", "/movie/upcoming");

            // Networks
            yield return new RequestDescriptor("Networks", $"/network/{IdTwentiethCenturyFox}");

            // People
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/movie_credits");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/tv_credits");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/combined_credits");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/external_ids");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/images");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/tagged_images");
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/changes");
            yield return new RequestDescriptor("People", "/person/popular");
            yield return new RequestDescriptor("People", "/person/latest");

            // Reviews
            yield return new RequestDescriptor("Reviews", $"/review/{IdTheDarkKnightRisesReviewId}");

            // Search
            yield return new RequestDescriptor("Search", "/search/company", new[] { Helpers.Create("query", "hbo") });
            yield return new RequestDescriptor("Search", "/search/collection", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/keyword", new[] { Helpers.Create("query", "tower") });
            yield return new RequestDescriptor("Search", "/search/list", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/movie", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/multi", new[] { Helpers.Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/person", new[] { Helpers.Create("query", "bruce") });
            yield return new RequestDescriptor("Search", "/search/tv", new[] { Helpers.Create("query", "house") });

            // Timezones
            yield return new RequestDescriptor("Timezones", "/timezones/list");

            // TV
            // /tv/id
            // /tv/id/account_states
            // /tv/id/alternative_titles
            // /tv/id/changes
            // /tv/id/content_ratings
            // /tv/id/credits
            // /tv/id/external_ids
            // /tv/id/images
            // /tv/id/keywords
            // /tv/id/rating
            // /tv/id/similar
            // /tv/id/translations
            // /tv/id/videos
            // /tv/latest
            // /tv/on_the_air
            // /tv/airing_today
            // /tv/top_rated
            // /tv/popular

            // TV Seasons
            // /tv/id/season/season_number
            // /tv/season/id/changes
            // /tv/id/season/season_number/account_states
            // /tv/id/season/season_number/credits
            // /tv/id/season/season_number/external_ids
            // /tv/id/season/season_number/images
            // /tv/id/season/season_number/videos

            // TV Episodes
            // /tv/id/season/season_number/episode/episode_number
            // /tv/episode/id/changes
            // /tv/id/season/season_number/episode/episode_number/account_states
            // /tv/id/season/season_number/episode/episode_number/credits
            // /tv/id/season/season_number/episode/episode_number/external_ids
            // /tv/id/season/season_number/episode/episode_number/images
            // /tv/id/season/season_number/episode/episode_number/rating
            // /tv/id/season/season_number/episode/episode_number/videos
        }
    }
}
