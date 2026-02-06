using Bynder.Sdk.Query.Decoder;

namespace Bynder.Sdk.Query.Asset
{
    /// <summary>
    ///
    /// </summary>
    /// <see cref="https://api.bynder.com/reference/get_api-v4-metaproperties-id-options-1"/>
    public class MetapropertyOptionQuery
    {
        #region Properties

        [ApiField("externalReference")]
        public string ExternalReference { get; set; }

        /// <summary>
        /// Limit of results per request. Max 1000. Default 50.
        /// </summary>
        [ApiField("limit")]
        public int? Limit { get; set; }

        [ApiField("name")]
        public string Name { get; set; }

        /// <summary>
        /// Page to be retrieved. Default 1
        /// </summary>
        [ApiField("page")]
        public int? Page { get; set; }

        #endregion Properties
    }
}