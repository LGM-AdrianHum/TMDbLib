using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;

namespace TMDbChangeDetector
{
    class RequestDescriptor
    {
        public RequestDescriptor(string category, string path, Type tmdbLibType)
            : this(category, path, HttpMethod.Get, null, tmdbLibType)
        {
        }

        public RequestDescriptor(string category, string path, IEnumerable<KeyValuePair<string, string>> postObject = null, Type tmdbLibType = null)
            : this(category, path, HttpMethod.Get, postObject, tmdbLibType)
        {
        }

        public RequestDescriptor(string category, string path, HttpMethod method, IEnumerable<KeyValuePair<string, string>> postObject = null, Type tmdbLibType = null)
        {
            Path = path;
            Method = method;

            PostObject = new NameValueCollection();
            if (postObject != null)
                foreach (KeyValuePair<string, string> pair in postObject)
                    PostObject.Add(pair.Key, pair.Value);

            Category = category;
            TmdbLibType = tmdbLibType;
        }

        public string Category { get; set; }

        public string Path { get; set; }

        public HttpMethod Method { get; set; }

        public NameValueCollection PostObject { get; set; }

        public Type TmdbLibType { get; set; }
    }
}