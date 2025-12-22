using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Models
{
    public class MediaTypeTransformConfig
    {
        [Newtonsoft.Json.JsonProperty("mediaType")]
        public string MediaType { get; set; }

        [Newtonsoft.Json.JsonProperty("filenameRegex")]
        public string FilenameRegex { get; set; }
    }
}
