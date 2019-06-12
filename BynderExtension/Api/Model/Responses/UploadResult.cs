using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class UploadResult
    {
        /// <summary>
        /// Media Id
        /// </summary>
        [JsonProperty("mediaid")]
        public string MediaId { get; set; }
    }
}
