using Newtonsoft.Json;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// User information.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// ProfileId
        /// </summary>
        [JsonProperty("profileId")]
        public string ProfileId { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
