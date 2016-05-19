using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private const string ApiKey = "c6b31d1cdad6a56a23f0c913e2482a31";
        private const string SessionId = "c413282cdadad9af972c06d9b13096a8b13ab1c1";
        private const string AccountId = "6089455";
        private const string ResponsesDirectory = "Responses";

        private static Uri BaseUri = new Uri("https://api.themoviedb.org/3/");

        static void Main(string[] args)
        {
            List<RequestDescriptor> xx = SetupMethods().ToList();

            foreach (RequestDescriptor descriptor in xx)
            {
                ProcessDescriptor(descriptor);

                Console.WriteLine();
            }
        }

        static void ProcessDescriptor(RequestDescriptor descriptor)
        {
            Console.WriteLine($"Processing {descriptor.Method} {descriptor.Path}");

            // Load previous
            JToken previous = Load(descriptor.Category, descriptor.Path);

            // Get current
            JToken current;
            try
            {
                current = GetCurrent(descriptor);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.ResetColor();
                return;
            }

            // Get differences to current
            TmdbDifferences diff = CalculateDiff(descriptor, previous, current);

            if (diff.IsSame)
            {
                Console.WriteLine("No differences");
            }
            else
            {
                Console.WriteLine($"DIFFERENT. New: {diff.KeysNew.Count:N0}, Removed: {diff.KeysOld.Count:N0}, Same: {diff.KeysSame.Count:N0}");
                Console.WriteLine();

                Console.WriteLine("New");
                Display(diff, diff.KeysNew);
                Console.WriteLine();

                Console.WriteLine("Removed");
                Display(diff, diff.KeysOld);
                Console.WriteLine();

                Console.WriteLine("Same");
                Display(diff, diff.KeysSame);
                Console.WriteLine();
            }

            // Store result
            Save(descriptor.Category, descriptor.Path, current);
        }

        static void Display(TmdbDifferences diff, IEnumerable<string> keys)
        {
            Func<string, JsonProperty> get = key => diff.OldProperties.ContainsKey(key) ? diff.OldProperties[key] : diff.NewProperties[key];

            foreach (string key in keys)
            {
                JsonProperty prop = get(key);

                Console.WriteLine($" {key} ({prop.Type})");
            }
        }

        static JToken GetCurrent(RequestDescriptor descriptor)
        {
            // Fetch current
            HttpResponseMessage response = IssueRequest(descriptor);

            CheckResponse(response);

            byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;
            string json = Encoding.UTF8.GetString(bytes);

            return JsonConvert.DeserializeObject<JToken>(json);
        }

        static TmdbDifferences CalculateDiff(RequestDescriptor descriptor, JToken prev, JToken curr)
        {
            TmdbDifferences result = new TmdbDifferences();
            result.Request = descriptor;

            // Compare
            result.OldProperties = IterateProperties(prev).ToDictionary(s => s.Path);
            result.NewProperties = IterateProperties(curr).ToDictionary(s => s.Path);

            result.KeysOld = new HashSet<string>(result.OldProperties.Keys);
            result.KeysOld.ExceptWith(result.NewProperties.Keys);

            result.KeysSame = new HashSet<string>(result.OldProperties.Keys);
            result.KeysSame.IntersectWith(result.NewProperties.Keys);

            result.KeysNew = new HashSet<string>(result.NewProperties.Keys);
            result.KeysNew.ExceptWith(result.OldProperties.Keys);

            return result;
        }

        static void CheckResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bad response on {response.RequestMessage.RequestUri}: {response.StatusCode}");

            StreamContent content = response.Content as StreamContent;
            if (content == null)
                throw new Exception($"Missing content for {response.RequestMessage.RequestUri}");

            if (content.Headers.ContentType.MediaType != "application/json")
                throw new Exception($"Content for {response.RequestMessage.RequestUri} had bad type, got: {content.Headers.ContentType.MediaType}");
        }

        static HttpResponseMessage IssueRequest(RequestDescriptor descriptor)
        {
            // Add ApiKey
            descriptor.PostObject["api_key"] = ApiKey;

            // Prep request
            Uri uri = new Uri(BaseUri, descriptor.Path.TrimStart('/'));
            string body = null;

            if (descriptor.Method == HttpMethod.Get)
            {
                // Put object in uri
                List<string> pairs = new List<string>();
                foreach (string key in descriptor.PostObject)
                {
                    string value = descriptor.PostObject[key];

                    pairs.Add(key + "=" + WebUtility.UrlEncode(value));
                }

                uri = new Uri(uri, "?" + string.Join("&", pairs));
            }
            else if (descriptor.Method == HttpMethod.Post)
            {
                JObject obj = new JObject();
                foreach (string key in descriptor.PostObject)
                {
                    string value = descriptor.PostObject[key];

                    obj[key] = value;
                }

                body = JsonConvert.SerializeObject(obj);
            }
            else
            {
                throw new InvalidOperationException($"Cannot use HTTP method: {descriptor.Method}");
            }

            HttpRequestMessage req = new HttpRequestMessage(descriptor.Method, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (body != null)
                req.Content = new StringContent(body);

            HttpResponseMessage resp;
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> tsk = client.SendAsync(req);

                try
                {
                    tsk.Wait();
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }

                resp = tsk.Result;
            }

            return resp;
        }

        static IEnumerable<RequestDescriptor> SetupMethods()
        {
            // Configuration
            yield return new RequestDescriptor("Configuration", "/configuration");

            // Account
            yield return new RequestDescriptor("Account", "/account", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/lists", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite/movies", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite/tv", new[] { Create("session_id", SessionId) });
            // TODO: POST yield return new RequestDescriptor("Account", $"/account/{AccountId}/favorite", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/movies", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/tv", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/rated/tv/episodes", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist/movies", new[] { Create("session_id", SessionId) });
            yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist/tv", new[] { Create("session_id", SessionId) });
            // TODO: POST  yield return new RequestDescriptor("Account", $"/account/{AccountId}/watchlist", new[] { Create("session_id", SessionId) });

            // Authentication
            // /authentication/token/new
            // /authentication/token/validate_with_login
            // /authentication/session/new
            // /authentication/guest_session/new

            // Certifications
            yield return new RequestDescriptor("Certifications", "/certification/movie/list");
            yield return new RequestDescriptor("Certifications", "/certification/tv/list");

            // Changes
            yield return new RequestDescriptor("Changes", "/movie/changes");
            yield return new RequestDescriptor("Changes", "/person/changes");
            yield return new RequestDescriptor("Changes", "/tv/changes");

            // Collections
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}");
            yield return new RequestDescriptor("Collections", $"/collection/{IdJamesBondCollection}/images");

            // Companies
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}");
            yield return new RequestDescriptor("Companies", $"/company/{IdTwentiethCenturyFox}/movies");

            // Credits
            yield return new RequestDescriptor("Credits", $"/credit/{IdBruceWillisMiamiVice}");

            // Discover
            yield return new RequestDescriptor("Discover", "/discover/movie");
            yield return new RequestDescriptor("Discover", "/discover/tv");

            // Find
            // /find/id

            // Genres
            yield return new RequestDescriptor("Genres", "/genre/movie/list");
            yield return new RequestDescriptor("Genres", "/genre/tv/list");
            yield return new RequestDescriptor("Genres", $"/genre/{IdGenreAction}/movies");

            // Guest Sessions
            // /guest_session/guest_session_id/rated/movies
            // /guest_session/guest_session_id/rated/tv
            // /guest_session/guest_session_id/rated/tv/episodes

            // Jobs
            yield return new RequestDescriptor("Jobs", "/job/list");

            // Keywords
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}");
            yield return new RequestDescriptor("Keywords", $"/keyword/{IdKeywordRogue}/movies");

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
            yield return new RequestDescriptor("Search", "/search/company", new[] { Create("query", "hbo") });
            yield return new RequestDescriptor("Search", "/search/collection", new[] { Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/keyword", new[] { Create("query", "tower") });
            yield return new RequestDescriptor("Search", "/search/list", new[] { Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/movie", new[] { Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/multi", new[] { Create("query", "james") });
            yield return new RequestDescriptor("Search", "/search/person", new[] { Create("query", "bruce") });
            yield return new RequestDescriptor("Search", "/search/tv", new[] { Create("query", "house") });

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

        private static IEnumerable<JsonProperty> IterateProperties(JToken obj)
        {
            return IterateProperties(null, obj);
        }

        static IEnumerable<JsonProperty> IterateProperties(string currentPath, JToken val)
        {
            if (val.Type == JTokenType.Array)
            {
                string path = currentPath;
                //if (!string.IsNullOrEmpty(currentPath))
                //    path += ".";

                path += "[array]";

                JArray arr = (JArray)val;

                // Iterate all childobjects, unique the resulting properties
                HashSet<string> childKeys = new HashSet<string>();
                List<JsonProperty> childs = new List<JsonProperty>();

                foreach (JObject token in ((JArray)arr).OfType<JObject>())
                {
                    foreach (JsonProperty childProp in IterateProperties(path, token))
                    {
                        if (childKeys.Add(childProp.Path))
                            childs.Add(childProp);
                    }
                }

                foreach (JsonProperty jsonProperty in childs)
                    yield return jsonProperty;
            }
            else if (val.Type == JTokenType.Object)
            {
                JObject obj = (JObject)val;
                foreach (JProperty property in obj.Properties())
                {
                    string name = property.Name;
                    JToken value = property.Value;

                    string path = currentPath;
                    if (!string.IsNullOrEmpty(currentPath))
                        path += ".";

                    path += name;

                    yield return new JsonProperty(path, name, value.Type, value);

                    foreach (JsonProperty jsonProperty in IterateProperties(path, value))
                    {
                        yield return jsonProperty;
                    }
                }
            }
        }

        static JToken Load(string category, string requestPath)
        {
            string filePath = Path.Combine(ResponsesDirectory, category + requestPath.Replace("/", "_") + ".json");

            if (File.Exists(filePath))
                return JsonConvert.DeserializeObject<JToken>(File.ReadAllText(filePath));

            return new JObject();
        }

        static void Save(string category, string requestPath, JToken obj)
        {
            if (!Directory.Exists(ResponsesDirectory))
                Directory.CreateDirectory(ResponsesDirectory);

            string filePath = Path.Combine(ResponsesDirectory, category + requestPath.Replace("/", "_") + ".json");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(obj));
        }

        static KeyValuePair<string, string> Create(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }
    }

    class TmdbDifferences
    {
        public RequestDescriptor Request { get; set; }

        public Dictionary<string, JsonProperty> OldProperties { get; set; }

        public Dictionary<string, JsonProperty> NewProperties { get; set; }

        public HashSet<string> KeysOld { get; set; }

        public HashSet<string> KeysSame { get; set; }

        public HashSet<string> KeysNew { get; set; }

        public bool IsSame => !KeysOld.Any() && !KeysNew.Any();
    }

    class JsonProperty
    {
        public JsonProperty(string path, string name, JTokenType type, object value)
        {
            Path = path;
            Name = name;
            Type = type;
            Value = value;
        }

        public string Path { get; set; }

        public string Name { get; set; }

        public JTokenType Type { get; set; }

        public object Value { get; set; }

        public override string ToString()
        {
            return $"{Path} ({Type})";
        }
    }
}
