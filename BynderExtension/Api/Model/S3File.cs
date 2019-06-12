using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class S3File
    {
        /// <summary>
        /// Upload id
        /// </summary>
        [JsonProperty("uploadid")]
        public string UploadId { get; set; }

        /// <summary>
        /// Target it
        /// </summary>
        [JsonProperty("targetid")]
        public string TargetId { get; set; }
    }
}
