using Bynder.Sdk.Api.Converters;
using Bynder.Sdk.Query.Decoder;
using Newtonsoft.Json;

namespace Bynder.Model
{
    public class PostRequestData
    {
        #region Properties

        [ApiField("data", Converter = typeof(ObjectToJsonStringConverter))]
        [JsonProperty("data")]
        public object Data { get; set; }

        #endregion Properties
    }
}