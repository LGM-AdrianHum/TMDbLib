using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMDbLib.Objects.Exceptions;
using TMDbLib.Objects.General;

namespace TMDbLib.Rest
{
    internal class RestResponse
    {
        protected readonly HttpResponseMessage Response;

        public TmdbStatusMessage Error { get; }

        public bool IsSuccessfull => Response.IsSuccessStatusCode;

        public bool IsNotFound => Response.StatusCode == HttpStatusCode.NotFound;

        public RestResponse(HttpResponseMessage response)
        {
            Response = response;

            if (!Response.IsSuccessStatusCode)
            {
                Task<string> content = GetContent();
                Task.WaitAll(content);

                Error = JsonConvert.DeserializeObject<TmdbStatusMessage>(content.Result);
            }
        }

        public HttpStatusCode StatusCode => Response.StatusCode;

        public string GetHeader(string name, string @default = null)
        {
            return Response.Headers.GetValues(name).FirstOrDefault() ?? @default;
        }

        public async Task<string> GetContent()
        {
            return await Response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }

    internal class RestResponse<T> : RestResponse
    {
        public RestResponse(HttpResponseMessage response)
            : base(response)
        {
        }

        public async Task<T> GetDataObject()
        {
            string content = await Response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(content);
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