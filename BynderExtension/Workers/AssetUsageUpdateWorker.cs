using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Workers
{
    using Bynder.SettingProviders;
    using Names;
    using Sdk.Query.Asset;
    using Utils.Helpers;
    using Utils.InRiver;
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    internal class AssetUsageUpdateWorker : AbstractBynderWorker, IWorker
    {
        #region Fields
        public override Dictionary<string, string> DefaultSettings => AssetUsageUpdateWorkerSettingsProvider.Create();
        #endregion Fields

        #region Constructors
        public AssetUsageUpdateWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null) :
            base(inRiverContext, bynderClient)
        {
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            // get settings, if missing return, nothing to do
            string integrationId = SettingHelper.GetInRiverIntegrationId(InRiverContext.Settings, InRiverContext.Logger);
            string inriverEntityUrl = SettingHelper.GetInRiverEntityUrl(InRiverContext.Settings, InRiverContext.Logger);

            if (string.IsNullOrWhiteSpace(integrationId) || string.IsNullOrWhiteSpace(inriverEntityUrl)) return;

            // get resource entity with fields
            resourceEntity =
                InRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);
            string assetId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;

            // check if empty - nothing to do
            if (string.IsNullOrEmpty(assetId)) return;

            string formattedInriverResourceUrl = inriverEntityUrl.Replace("{entityId}", resourceEntity.Id.ToString());

            // clear all current usages
            InRiverContext.Log(LogLevel.Information, $"Set asset usage for asset {assetId}");
            _bynderClient.GetAssetService().DeleteAssetUsage(new AssetUsageQuery(integrationId, assetId));

            // and set new one
            _bynderClient.GetAssetService().CreateAssetUsage(new AssetUsageQuery(integrationId, assetId)
            {
                Uri = formattedInriverResourceUrl
            });
        }

        #endregion Methods
    }
}