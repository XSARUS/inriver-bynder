using Newtonsoft.Json;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// User information.
    /// </summary>
    public class User
    {
        #region Properties

        /// <summary>
        /// Email
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

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

        #endregion Properties
    }
}