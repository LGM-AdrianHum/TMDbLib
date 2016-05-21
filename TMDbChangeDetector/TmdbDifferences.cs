using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace TMDbChangeDetector
{
    class TmdbDifferences
    {
        public TmdbDifferences()
        {
            NewFields = new Dictionary<string, ErrorEventArgs>();
            MissingFields = new Dictionary<string, ErrorEventArgs>();
        }

        public RequestDescriptor Request { get; set; }

        public Dictionary<string, ErrorEventArgs> NewFields { get; set; }

        public Dictionary<string, ErrorEventArgs> MissingFields { get; set; }

        public bool IsSame => !NewFields.Any() && !MissingFields.Any();
    }
}