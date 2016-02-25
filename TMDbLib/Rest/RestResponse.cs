using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMDbLib.Objects.General;

namespace TMDbLib.Rest
{
    internal class RestResponse
    {
        private readonly HttpResponseHeaders _headers;
        private readonly string _responseContent;

        public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

        public HttpStatusCode StatusCode { get; }

        public bool IsSuccessStatusCode { get; set; }

        public TmdbStatusMessage ErrorMessage { get; set; }

        internal RestResponse(HttpResponseHeaders headers, HttpStatusCode statusCode, bool isSuccessStatusCode, string responseContent, TmdbStatusMessage errorMessage)
        {
            _headers = headers;
            _responseContent = responseContent;

            StatusCode = statusCode;
            IsSuccessStatusCode = isSuccessStatusCode;
            ErrorMessage = errorMessage;
        }

        public string GetHeader(string name, string @default = null)
        {
            return _headers.GetValues(name).FirstOrDefault() ?? @default;
        }

        public async Task<string> GetContent()
        {
            return _responseContent;
        }
    }

    internal class RestResponse<T> : RestResponse
    {
        internal RestResponse(HttpResponseHeaders headers, HttpStatusCode statusCode, bool isSuccessStatusCode, string responseContent, TmdbStatusMessage errorMessage)
            : base(headers, statusCode, isSuccessStatusCode, responseContent, errorMessage)
        {
        }

        public async Task<T> GetDataObject()
        {
            return JsonConvert.DeserializeObject<T>(await GetContent());
        }

        public static implicit operator T(RestResponse<T> response)
        {
            try
            {

                return response.GetDataObject().Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}