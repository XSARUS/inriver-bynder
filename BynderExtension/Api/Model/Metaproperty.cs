using Bynder.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class Metaproperty
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public List<MetapropertyMetapropertyOption> Options { get; set; }
        public string Id { get; set; }
        [JsonProperty("IsMultiselect")]
        public bool IsMultiSelect { get; set; }

        [JsonProperty("IsMultifilter")]
        public bool IsMultiFilter { get; set; }
        public bool IsRequired { get; set; }
        public bool IsApiField { get; set; }
        [JsonProperty("zindex")]
        public int ZIndex { get; set; }
        public bool ShowInGridView { get; set; }
        public bool ShowInListView { get; set; }
        [JsonProperty("IsMainfilter")]
        public bool IsMainFilter { get; set; }
        public bool IsSearchable { get; set; }
        public bool IsDrilldown { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsEditable { get; set; }
        /// <summary>
        /// Should actually be an enum...
        /// </summary>
        public MetapropertyType Type { get; set; }
        public bool UseDependencies { get; set; }

    }
}
