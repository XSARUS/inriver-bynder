using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bynder.Api.Model
{
    public class PollResponse
    {
        #region Properties

        /// <summary>
        /// Returns the items for which the conversion succeeded.
        /// </summary>
        [JsonProperty("itemsDone")]
        public HashSet<string> ItemsDone { get; set; }

        /// <summary>
        /// Returns the items for which the conversion failed
        /// </summary>
        [JsonProperty("itemsFailed")]
        public HashSet<string> ItemsFailed { get; set; }

        #endregion Properties
    }
}