using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class UploadRequest
    {
        #region Properties

        /// <summary>
        /// Amazon parameters information <see cref="MultipartParameters"/>
        /// </summary>
        [JsonProperty("multipart_params")]
        public MultipartParameters MultipartParams { get; set; }

        /// <summary>
        /// S3 file information. <see cref="S3File"/>
        /// </summary>
        [JsonProperty("s3file")]
        public S3File S3File { get; set; }

        /// <summary>
        /// S3 file name
        /// </summary>
        [JsonProperty("s3_filename")]
        public string S3Filename { get; set; }

        #endregion Properties
    }
}