using System.Collections.Generic;
using System.Linq;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Metaproperties on the asset
    /// </summary>
    public class AssetMetapropertyList : List<AssetMetaproperty>
    {
        #region Constructors

        public AssetMetapropertyList(IEnumerable<AssetMetaproperty> metaproperties) : base(metaproperties)
        {
        }

        public AssetMetapropertyList()
        { }

        #endregion Constructors
    }
}