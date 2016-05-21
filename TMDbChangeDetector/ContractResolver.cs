using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TMDbChangeDetector
{
    internal class ContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty res = base.CreateProperty(member, memberSerialization);

            res.Required = Required.AllowNull;

            return res;
        }
    }
}