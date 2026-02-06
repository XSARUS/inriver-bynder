using Newtonsoft.Json;

namespace Bynder.Model
{
    public class RequestData
    {
        #region Properties

        [JsonProperty("data")]
        public object Data { get; set; }

        #endregion Properties
    }
}