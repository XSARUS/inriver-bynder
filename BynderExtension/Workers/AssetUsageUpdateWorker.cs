using Bynder.Api;
using Bynder.Names;
using Bynder.Utils.InRiver;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    class AssetUsageUpdateWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly IBynderClient _bynderBynderClient;

        public AssetUsageUpdateWorker(inRiverContext inRiverContext, IBynderClient bynderBynderClient)
        {
            _inRiverContext = inRiverContext;
            _bynderBynderClient = bynderBynderClient;
        }

        public void Execute(Entity resourceEntity)
        {
            // get resource entity with fields
            resourceEntity =
                _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);
            string assetId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;

            // check if empty - nothing to do
            if (string.IsNullOrEmpty(assetId)) return;

            // get settings, if missing return, nothing to do
            var settings = _inRiverContext.Settings;
            if (!settings.TryGetValue(Config.Settings.inriverIntegrationId, out var integrationId) ||
                !settings.TryGetValue(Config.Settings.inriverEntityUrl, out var inriverEntityUrl)) return;

            string formattedInriverResourceUrl = inriverEntityUrl.Replace("{entityId}", resourceEntity.Id.ToString());

            // clear all current usages
            _inRiverContext.Logger.Log(LogLevel.Information, $"Set asset usage for asset {assetId}");
            _bynderBynderClient.DeleteAssetUsage(assetId, integrationId);

            // and set new one
            _bynderBynderClient.CreateAssetUsage(assetId, integrationId, formattedInriverResourceUrl);
        }
    }
}