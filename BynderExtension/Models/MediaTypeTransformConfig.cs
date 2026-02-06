namespace Bynder.Models
{
    public class MediaTypeTransformConfig
    {
        #region Properties

        [Newtonsoft.Json.JsonProperty("filenameRegex")]
        public string FilenameRegex { get; set; }

        [Newtonsoft.Json.JsonProperty("mediaType")]
        public string MediaType { get; set; }

        #endregion Properties
    }
}