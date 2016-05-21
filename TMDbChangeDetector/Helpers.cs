using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TMDbChangeDetector
{
    static class Helpers
    {
        static readonly Regex NormalizeRegex = new Regex(@"\[[\d]+\]", RegexOptions.Compiled);

        public static string GetJson(RequestDescriptor descriptor)
        {
            // Fetch current
            HttpResponseMessage response = IssueRequest(descriptor);

            CheckResponse(response);

            byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;

            return Encoding.UTF8.GetString(bytes);
        }

        public static string NormalizeErrorKey(string key)
        {
            return NormalizeRegex.Replace(key, "[array]");
        }

        private static void CheckResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bad response on {response.RequestMessage.RequestUri}: {response.StatusCode}");

            StreamContent content = response.Content as StreamContent;
            if (content == null)
                throw new Exception($"Missing content for {response.RequestMessage.RequestUri}");

            if (content.Headers.ContentType.MediaType != "application/json")
                throw new Exception($"Content for {response.RequestMessage.RequestUri} had bad type, got: {content.Headers.ContentType.MediaType}");
        }

        private static HttpResponseMessage IssueRequest(RequestDescriptor descriptor)
        {
            // Add ApiKey
            descriptor.PostObject["api_key"] = Program.ApiKey;

            // Prep request
            Uri uri = new Uri(Program.BaseUri, descriptor.Path.TrimStart('/'));
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

                uri = new Uri(uri, "?" + String.Join("&", pairs));
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

        public static KeyValuePair<string, string> Create(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        public static string PrettyPrintType(Type type)
        {
            if (type.IsGenericType)
            {
                // Type<params, ..>
                string parms = String.Join(", ", type.GetGenericArguments().Select(PrettyPrintType));
                string cleanName = type.Name.Substring(0, type.Name.IndexOf('`'));

                return $"{cleanName}<{parms}>";
            }
            else
            {
                // Type
                return type.Name;
            }
        }
    }
}