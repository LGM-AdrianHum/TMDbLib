using Newtonsoft.Json;

namespace TMDbLib.Objects.General
{
    public class TmdbStatusMessage
    {
        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }
    }
}