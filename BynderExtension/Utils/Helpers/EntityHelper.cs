using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace Bynder.Utils.Helpers
{
    using Enums;
    using Names;
    using Sdk.Model;

    public static class EntityHelper
    {
        #region Methods

        public static Entity GetResourceByAsset(Media asset, ResourceSearchType resourceSearchType, inRiverContext context, LoadLevel loadLevel)
        {
            switch (resourceSearchType)
            {
                case ResourceSearchType.Filename:
                    context.Log(LogLevel.Debug, $"Search by 'Filename' (enum-value {ResourceSearchType.Filename}) for asset {asset.Id} on FieldTypeId '{FieldTypeIds.ResourceFilename}' and value '{asset.GetOriginalFileName()}'");
                    return context.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceFilename, asset.GetOriginalFileName(), loadLevel);

                case ResourceSearchType.PrefixedFilename:
                    context.Log(LogLevel.Debug, $"Search by 'PrefixedFilename' (enum-value {ResourceSearchType.PrefixedFilename}) for asset {asset.Id} on FieldTypeId '{FieldTypeIds.ResourceFilename}' and value '{asset.Id}_{asset.GetOriginalFileName()}'");
                    return context.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceFilename, asset.Id + '_' + asset.GetOriginalFileName(), loadLevel);

                case ResourceSearchType.AssetId:
                default:
                    context.Log(LogLevel.Debug, $"Search by 'AssetId' (enum-value {ResourceSearchType.AssetId}) for asset {asset.Id} on FieldTypeId '{FieldTypeIds.ResourceBynderId}' and value '{asset.Id}'");
                    return context.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceBynderId, asset.Id, loadLevel);
            }
        }

        #endregion Methods
    }
}