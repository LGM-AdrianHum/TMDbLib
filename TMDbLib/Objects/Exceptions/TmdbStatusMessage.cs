using Newtonsoft.Json;

namespace TMDbLib.Objects.Exceptions
{
    public class TmdbStatusMessage
    {
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }
    }
}