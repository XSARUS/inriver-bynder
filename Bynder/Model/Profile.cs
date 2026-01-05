using Newtonsoft.Json;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Profile information.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
