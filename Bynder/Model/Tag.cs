using Newtonsoft.Json;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model describing a tag
    /// </summary>
    public class Tag
    {
        #region Properties

        /// <summary>
        /// Tag ID
        /// </summary>
        [JsonProperty("id")]
        public string ID { get; set; }

        /// <summary>
        /// Number of media that are used by tag
        /// </summary>
        [JsonProperty("mediaCount")]
        public int MediaCount { get; set; }

        /// <summary>
        /// Tag Name
        /// </summary>
        [JsonProperty("tag")]
        public string TagName { get; set; }

        #endregion Properties
    }
}