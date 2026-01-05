using Newtonsoft.Json;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model for the response of the API.
    /// </summary>
    public class MetapropertyOptionStatus
    {
        /// <summary>
        /// Id from API
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Message from API
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Status code
        /// </summary>
        [JsonProperty("statuscode")]
        public int StatusCode { get; set; }
    }
}
