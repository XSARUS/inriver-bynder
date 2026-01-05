using Bynder.Sdk.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Model
{
    public class RequestData
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
