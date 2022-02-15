using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class MultipartParameters
    {
        #region Properties

        /// <summary>
        /// Acl
        /// </summary>
        [JsonProperty("acl")]
        public string Acl { get; set; }

        /// <summary>
        /// Amz algorithm
        /// </summary>
        [JsonProperty("x-amz-algorithm")]
        public string Algorithm { get; set; }

        /// <summary>
        /// Amz credentials
        /// </summary>
        [JsonProperty("x-amz-credential")]
        public string AWSAccessKeyid { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        [JsonProperty("Content-Type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Amz date
        /// </summary>
        [JsonProperty("x-amz-date")]
        public string Date { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Policy
        /// </summary>
        [JsonProperty("Policy")]
        public string Policy { get; set; }

        /// <summary>
        /// Amz signature
        /// </summary>
        [JsonProperty("X-Amz-Signature")]
        public string Signature { get; set; }

        /// <summary>
        /// Success status
        /// </summary>
        [JsonProperty("success_action_status")]
        public string SuccessActionStatus { get; set; }

        #endregion Properties
    }
}