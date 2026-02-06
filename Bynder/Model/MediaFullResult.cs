using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bynder.Sdk.Model
{
    public class Count
    {
        #region Properties

        [JsonProperty("total")]
        public long Total { get; set; }

        #endregion Properties
    }
    public class MediaFullResult
    {
        #region Properties

        [JsonProperty("count")]
        public Count Count { get; set; }

        [JsonProperty("media")]
        public IList<Media> Media { get; set; }

        #endregion Properties
    }
}