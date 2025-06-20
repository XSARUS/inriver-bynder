using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    using Api;
    using Names;
    using Utils.Helpers;
    using Utils.InRiver;

    internal class AssetUsageUpdateWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderBynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetUsageUpdateWorker(inRiverContext inRiverContext, IBynderClient bynderBynderClient)
        {
            _inRiverContext = inRiverContext;
            _bynderBynderClient = bynderBynderClient;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            // get settings, if missing return, nothing to do
            string integrationId = SettingHelper.GetInRiverIntegrationId(_inRiverContext.Settings, _inRiverContext.Logger);
            string inriverEntityUrl = SettingHelper.GetInRiverEntityUrl(_inRiverContext.Settings, _inRiverContext.Logger);

            if (string.IsNullOrWhiteSpace(integrationId) || string.IsNullOrWhiteSpace(inriverEntityUrl)) return;

            // get resource entity with fields
            resourceEntity =
                _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);
            string assetId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;

            // check if empty - nothing to do
            if (string.IsNullOrEmpty(assetId)) return;

            string formattedInriverResourceUrl = inriverEntityUrl.Replace("{entityId}", resourceEntity.Id.ToString());

            // clear all current usages
            _inRiverContext.Logger.Log(LogLevel.Information, $"Set asset usage for asset {assetId}");
            _bynderBynderClient.DeleteAssetUsage(assetId, integrationId);

            // and set new one
            _bynderBynderClient.CreateAssetUsage(assetId, integrationId, formattedInriverResourceUrl);
        }

        #endregion Methods
    }
}