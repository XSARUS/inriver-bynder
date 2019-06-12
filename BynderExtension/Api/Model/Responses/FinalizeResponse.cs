using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class FinalizeResponse
    {
        /// <summary>
        /// Import id for the upload. Needed to poll and save media.
        /// </summary>
        [JsonProperty("importId")]
        public string ImportId { get; set; }
    }
}
