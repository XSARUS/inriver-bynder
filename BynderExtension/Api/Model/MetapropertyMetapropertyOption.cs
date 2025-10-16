using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    /// <summary>
    /// This is the metaproperty representation of the metaproperty option
    /// </summary>
    public class MetapropertyMetapropertyOption : MetapropertyOption
    {
        [JsonProperty("zindex")]
        public int ZIndex { get; set; }
        public int? PregeneratedZipFileSize { get; set; }
        public List<string> LinkedOptionIds { get; set; }
        public bool IsSelectable { get; set; }
        [JsonProperty("product_suffix")]
        public string ProductSuffix { get; set; }
        public bool Active { get; set; }
        public Dictionary<string, string> Descriptions { get; set; }
        public string Description { get; set; }
        public bool HideByDefault { get; set; }
        /// <summary>
        /// URL to Bynder CDN
        /// </summary>
        public string Image { get; set; }
        public string ParentId { get; set; }
        public string ExternalRefeference { get; set; }
    }
}
