using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Sdk.Model
{
    public class MediaFullResult
    {
        [JsonProperty("count")]
        public Count Count { get; set; }

        [JsonProperty("media")]
        public IList<Media> Media { get; set; }
    }
    public class Count
    {
        [JsonProperty("total")]
        public long Total { get; set; }
    }
}
