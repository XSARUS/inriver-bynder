namespace Bynder.Sdk.Query.Asset
{
    public class MetapropertyQuery
    {
        #region Properties

        /// <summary>
        /// Id of the media to modify
        /// </summary>
        public string MetapropertyId { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes the class with required information
        /// </summary>
        /// <param name="metapropertyId">The metadata to be modified</param>
        public MetapropertyQuery(string metapropertyId)
        {
            MetapropertyId = metapropertyId;
        }

        #endregion Constructors
    }
}