using Bynder.Sdk.Api.Converters;
using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Decoder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Model
{
    public class PostRequestData
    {
        [ApiField("data", Converter = typeof(ObjectToJsonStringConverter))]
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
