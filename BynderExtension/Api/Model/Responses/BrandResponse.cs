using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class BrandResponse
    {
        #region Properties

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        #endregion Properties
    }
}