using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class MetapropertyOptionCU
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        [JsonProperty("zindex")]
        public int? ZIndex { get; set; }
        public bool? IsSelectable { get; set; }
        public string ParentId { get; set; }
        public List<MetapropertyOptionCU> Options { get; set; }
    }
}
