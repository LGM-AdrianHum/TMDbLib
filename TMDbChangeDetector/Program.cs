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
            yield return new RequestDescriptor("Configuration", "/configuration", typeof(TMDbConfig));

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
            yield return new RequestDescriptor("Certifications", "/certification/movie/list", typeof(CertificationsContainer));
            yield return new RequestDescriptor("Certifications", "/certification/tv/list", typeof(CertificationsContainer));

            // Changes
            yield return new RequestDescriptor("Changes", "/movie/changes", typeof(SearchContainer<ChangesListItem>));
            yield return new RequestDescriptor("Changes", "/person/changes", typeof(SearchContainer<ChangesListItem>));
            yield return new RequestDescriptor("Changes", "/tv/changes", typeof(SearchContainer<ChangesListItem>));

            // Collections
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}", typeof(Collection));
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}/images", typeof(ImagesWithId));

            // Companies
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}", typeof(Company));
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}/movies", typeof(SearchContainerWithId<MovieResult>));

            // Credits
            yield return new RequestDescriptor("Credits", $"/credit/{IdBruceWillisMiamiVice}", typeof(Credit));

            // Discover
            yield return new RequestDescriptor("Discover", "/discover/movie", typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Discover", "/discover/tv", typeof(SearchContainer<SearchTv>));

            // Find
            yield return new RequestDescriptor("Find", $"/find/{ImdbBreakingBadId}", new[] { Helpers.Create("external_source", FindExternalSource.Imdb.GetDescription()) }, typeof(FindContainer));

            // Genres
            yield return new RequestDescriptor("Genres", "/genre/movie/list", typeof(GenreContainer));
            yield return new RequestDescriptor("Genres", "/genre/tv/list", typeof(GenreContainer));
            yield return new RequestDescriptor("Genres", $"/genre/{IdGenreAction}/movies", typeof(SearchContainerWithId<MovieResult>));

            // Guest Sessions
            yield return new RequestDescriptor("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/movies", typeof(SearchContainer<MovieWithRating>));
            yield return new RequestDescriptor("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/tv", typeof(SearchContainer<TvShowWithRating>));
            yield return new RequestDescriptor("Guest Sessions", $"/guest_session/{GuestTestSessionId}/rated/tv/episodes", typeof(SearchContainer<TvEpisodeWithRating>));

            // Jobs
            yield return new RequestDescriptor("Jobs", "/job/list", typeof(JobContainer));

            // Keywords
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}", typeof(Keyword));
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}/movies", typeof(SearchContainer<MovieResult>));

            // Lists
            yield return new RequestDescriptor("Lists", $"/list/{TestListId}", typeof(List));
            yield return new RequestDescriptor("Lists", $"/list/{TestListId}/item_status", new[] { Helpers.Create("movie_id", IdAvatar.ToString()) }, typeof(ListStatus));

            // TODO: /list
            // TODO: /list/id/add_item
            // TODO: /list/id/remove_item
            // TODO: /list/id/clear

            // Movies
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}", typeof(Movie));
            TODO: yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/account_states", new[] { Helpers.Create("session_id", SessionId) }, typeof(AccountState));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/alternative_titles", typeof(AlternativeTitles));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/credits", typeof(Credits));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/images", typeof(Images));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/keywords", typeof(KeywordsContainer));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/release_dates", typeof(Releases));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/videos", typeof(ResultContainer<Video>));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/translations", typeof(TranslationsContainer));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/similar", typeof(SearchContainer<MovieResult>));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/reviews", typeof(SearchContainer<Review>));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/lists", typeof(SearchContainer<ListResult>));
            yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/changes", typeof(ChangesContainer));
            // TODO: yield return new RequestDescriptor("Movies", $"/movie/{IdAvatar}/rating", tmdbLibType: typeof(object));
            yield return new RequestDescriptor("Movies", "/movie/latest", typeof(Movie));
            yield return new RequestDescriptor("Movies", "/movie/now_playing", typeof(SearchContainer<MovieResult>));
            yield return new RequestDescriptor("Movies", "/movie/popular", typeof(SearchContainer<MovieResult>));
            yield return new RequestDescriptor("Movies", "/movie/top_rated", typeof(SearchContainer<MovieResult>));
            yield return new RequestDescriptor("Movies", "/movie/upcoming", typeof(SearchContainer<MovieResult>));

            // Networks
            yield return new RequestDescriptor("Networks", $"/network/{IdTwentiethCenturyFox}", typeof(Network));

            // People
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}", typeof(Person));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/movie_credits", typeof(MovieCredits));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/tv_credits", typeof(TvCredits));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/combined_credits", typeof(object));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/external_ids", typeof(ExternalIds));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/images", typeof(ProfileImages));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/tagged_images", typeof(SearchContainer<TaggedImage>));
            yield return new RequestDescriptor("People", $"/person/{IdBruceWillis}/changes", typeof(ChangesContainer));
            yield return new RequestDescriptor("People", "/person/popular", typeof(SearchContainer<PersonResult>));
            yield return new RequestDescriptor("People", "/person/latest", typeof(Person));

            // Reviews
            yield return new RequestDescriptor("Reviews", $"/review/{IdTheDarkKnightRisesReviewId}", typeof(Review));

            // Search
            yield return new RequestDescriptor("Search", "/search/company", new[] { Helpers.Create("query", "hbo") }, typeof(SearchContainer<SearchCompany>));
            yield return new RequestDescriptor("Search", "/search/collection", new[] { Helpers.Create("query", "james") }, typeof(SearchContainer<SearchResultCollection>));
            yield return new RequestDescriptor("Search", "/search/keyword", new[] { Helpers.Create("query", "tower") }, typeof(SearchContainer<SearchKeyword>));
            yield return new RequestDescriptor("Search", "/search/list", new[] { Helpers.Create("query", "james") }, typeof(SearchContainer<SearchList>));
            yield return new RequestDescriptor("Search", "/search/movie", new[] { Helpers.Create("query", "james") }, typeof(SearchContainer<SearchMovie>));
            yield return new RequestDescriptor("Search", "/search/multi", new[] { Helpers.Create("query", "james") }, typeof(SearchContainer<SearchMulti>));
            yield return new RequestDescriptor("Search", "/search/person", new[] { Helpers.Create("query", "bruce") }, typeof(SearchContainer<SearchPerson>));
            yield return new RequestDescriptor("Search", "/search/tv", new[] { Helpers.Create("query", "house") }, typeof(SearchContainer<SearchTv>));

            // Timezones
            yield return new RequestDescriptor("Timezones", "/timezones/list", typeof(Timezones));

            // TV
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}", typeof(TvShow));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/account_states", new[] { Helpers.Create("session_id", SessionId) }, typeof(AccountState));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/alternative_titles", typeof(ResultContainer<AlternativeTitle>));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/changes", typeof(ChangesContainer));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/content_ratings", typeof(ResultContainer<ContentRating>));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/credits", typeof(Credits));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/external_ids", typeof(ExternalIds));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/images", typeof(ImagesWithId));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/keywords", typeof(ResultContainer<Keyword>));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/rating", typeof(ResultContainer<ContentRating>));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/similar", typeof(SearchContainer<SearchTv>));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/translations", typeof(TranslationsContainer));
            yield return new RequestDescriptor("TV", $"/tv/{IdBreakingBad}/videos", typeof(ResultContainer<Video>));
            yield return new RequestDescriptor("TV", "/tv/latest", typeof(TvShow));
            yield return new RequestDescriptor("TV", "/tv/on_the_air", typeof(SearchContainer<TvShow>));
            yield return new RequestDescriptor("TV", "/tv/airing_today", typeof(SearchContainer<TvShow>));
            yield return new RequestDescriptor("TV", "/tv/top_rated", typeof(SearchContainer<TvShow>));
            yield return new RequestDescriptor("TV", "/tv/popular", typeof(SearchContainer<TvShow>));

            // TV Seasons
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1", typeof(TvSeason));
            yield return new RequestDescriptor("TV Seasons", $"/tv/season/{IdBreakingBadSeason1}/changes", typeof(ChangesContainer));
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1/account_states", new[] { Helpers.Create("session_id", SessionId) }, typeof(ResultContainer<TvEpisodeAccountState>));
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1/credits", typeof(Credits));
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1/external_ids", typeof(ExternalIds));
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1/images", typeof(PosterImages));
            yield return new RequestDescriptor("TV Seasons", $"/tv/{IdBreakingBad}/season/1/videos", typeof(ResultContainer<Video>));

            // TV Episodes
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1", typeof(TvEpisode));
            yield return new RequestDescriptor("TV Episodes", $"/tv/episode/{IdBreakingBadSeason1Episode1}/changes", typeof(ChangesContainer));
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/account_states", new[] { Helpers.Create("session_id", SessionId) }, typeof(TvEpisodeAccountState));
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/credits", typeof(Credits));
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/external_ids", typeof(ExternalIds));
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/images", typeof(StillImages));
            // TODO: yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/rating", typeof(TvShow));
            yield return new RequestDescriptor("TV Episodes", $"/tv/{IdBreakingBad}/season/1/episode/1/videos", typeof(ResultContainer<Video>));
        }
    }
}
