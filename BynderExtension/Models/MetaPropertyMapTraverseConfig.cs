using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bynder.Models
{
    using Utils.Extensions;

    public class MetaPropertyMapTraverseConfig
    {
        #region Properties

        [JsonProperty("entityTypeId")]
        public string EntityTypeId { get; set; } = string.Empty;

        /// <summary>
        /// If null then all fieldsets, if empty then no fieldset, if filled then that fieldset
        /// </summary>
        [JsonProperty("fieldSet")]
        public string FieldSet { get; set; }

        [JsonProperty("inbound")]
        public List<MetaPropertyMapTraverseConfig> Inbound { get; set; } = new List<MetaPropertyMapTraverseConfig>();

        [JsonProperty("linkTypeId")]
        public string LinkTypeId { get; set; } = string.Empty;

        [JsonProperty("metaPropertyMapping")]
        public List<MetaPropertyMap> MetaPropertyMapping { get; set; } = new List<MetaPropertyMap>();

        [JsonProperty("outbound")]
        public List<MetaPropertyMapTraverseConfig> Outbound { get; set; } = new List<MetaPropertyMapTraverseConfig>();

        #endregion Properties

        #region Methods

        /// <summary>
        /// Return a generated hash for specific properties so it can be used in a Comparer
        /// </summary>
        public virtual string Hash()
        {
            var input = new StringBuilder();

            input.Append(EntityTypeId ?? string.Empty);
            input.Append(FieldSet ?? string.Empty);
            input.Append(LinkTypeId ?? string.Empty);

            if (Inbound != null && Inbound.Any())
            {
                var inboundHashes = Inbound.Select(item => item.Hash().ToString()).OrderBy(h => h);
                input.Append(string.Join(string.Empty, inboundHashes));
            }

            if (Outbound != null && Outbound.Any())
            {
                var outboundHashes = Outbound.Select(item => item.Hash().ToString()).OrderBy(h => h);
                input.Append(string.Join(string.Empty, outboundHashes));
            }

            return input.ToString().ToMd5Hash();
        }

        #endregion Methods

    }
}
