using System.Collections.Generic;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Metaproperty on the asset
    /// </summary>
    public class AssetMetaproperty
    {
        #region Properties

        /// <summary>
        /// Unique GUID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the metaproperty, should be alphanumeric only. Cannot be modified after the metaproperty has been created.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Values from bynder are always represented as array
        /// </summary>
        public List<string> Values { get; set; }

        #endregion Properties
    }
}