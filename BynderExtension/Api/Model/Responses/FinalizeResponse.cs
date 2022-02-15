using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class FinalizeResponse
    {
        #region Properties

        /// <summary>
        /// Import id for the upload. Needed to poll and save media.
        /// </summary>
        [JsonProperty("importId")]
        public string ImportId { get; set; }

        #endregion Properties
    }
}