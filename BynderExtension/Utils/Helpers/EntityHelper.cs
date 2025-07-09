using inRiver.Remoting;
using inRiver.Remoting.Objects;

namespace Bynder.Utils.Helpers
{
    using Api.Model;
    using Enums;
    using Names;

    public static class EntityHelper
    {
        #region Methods

        public static Entity GetResourceByAsset(Asset asset, ResourceSearchType resourceSearchType, IDataService dataService, LoadLevel loadLevel)
        {
            switch (resourceSearchType)
            {
                case ResourceSearchType.Filename:
                    return dataService.GetEntityByUniqueValue(FieldTypeIds.ResourceFilename, asset.GetOriginalFileName(), loadLevel);

                case ResourceSearchType.PrefixedFilename:
                    return dataService.GetEntityByUniqueValue(FieldTypeIds.ResourceFilename, asset.Id + '_' + asset.GetOriginalFileName(), loadLevel);

                case ResourceSearchType.AssetId:
                default:
                    return dataService.GetEntityByUniqueValue(FieldTypeIds.ResourceBynderId, asset.Id, loadLevel);
            }
        }

        #endregion Methods
    }
}