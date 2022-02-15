using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class UploadResult
    {
        #region Properties

        /// <summary>
        /// Media Id
        /// </summary>
        [JsonProperty("mediaid")]
        public string MediaId { get; set; }

        #endregion Properties
    }
}