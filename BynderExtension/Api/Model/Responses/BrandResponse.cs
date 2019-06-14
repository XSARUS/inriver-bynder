using Newtonsoft.Json;

namespace Bynder.Api.Model
{
    public class BrandResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
    }
}
