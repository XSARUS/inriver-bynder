using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bynder.Workers
{
    using Bynder.SettingProviders;
    using Names;
    using Sdk.Query.Asset;
    using Utils.Helpers;
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    internal class AssetUsageUpdateWorker : AbstractBynderWorker, IWorker
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings => AssetUsageUpdateWorkerSettingsProvider.Create();

        #endregion Properties

        #region Constructors

        public AssetUsageUpdateWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null) :
            base(inRiverContext, bynderClient)
        {
        }

        #endregion Constructors

        #region Methods

        public async Task Execute(Entity resourceEntity)
        {
            // get settings, if missing return, nothing to do
            string integrationId = SettingHelper.GetInRiverIntegrationId(InRiverContext.Settings, InRiverContext.Logger);
            string inriverEntityUrl = SettingHelper.GetInRiverEntityUrl(InRiverContext.Settings, InRiverContext.Logger);

            if (string.IsNullOrWhiteSpace(integrationId) || string.IsNullOrWhiteSpace(inriverEntityUrl)) return;

            string assetId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;

            // check if empty - nothing to do
            if (string.IsNullOrEmpty(assetId)) return;

            string formattedInriverResourceUrl = inriverEntityUrl.Replace("{entityId}", resourceEntity.Id.ToString());

            // clear all current usages
            InRiverContext.Log(LogLevel.Information, $"Set asset usage for asset {assetId}");
            await _bynderClient.GetAssetService().DeleteAssetUsage(new AssetUsageQuery(integrationId, assetId));

            // and set new one
            await _bynderClient.GetAssetService().CreateAssetUsage(new AssetUsageQuery(integrationId, assetId)
            {
                Uri = formattedInriverResourceUrl
            });
        }

        #endregion Methods
    }
}