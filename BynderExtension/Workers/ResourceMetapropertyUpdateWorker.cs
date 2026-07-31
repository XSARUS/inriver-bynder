using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bynder.Workers
{
    using Bynder.Utils.Traverser;
    using Models;
    using Names;
    using Sdk.Query.Asset;
    using Sdk.Service;
    using SettingProviders;
    using Utils.Extensions;
    using Utils.Helpers;

    /// <summary>
    /// Updates metaproperties on assets in Bynder if Entity applies to EXPORT_CONDITIONS
    /// </summary>
    public class ResourceMetapropertyUpdateWorker : AbstractBynderUploadWorker, IWorker
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings => ResourceMetapropertyUpdateWorkerSettingsProvider.Create();
        private readonly MetapropertyMapTraverser _metapropertyMapTraverser;

        #endregion Properties

        #region Constructors

        public ResourceMetapropertyUpdateWorker(MetapropertyMapTraverser metapropertyMapTraverser, inRiverContext inRiverContext, IBynderClient bynderClient = null) : base(inRiverContext, bynderClient)
        {
            _metapropertyMapTraverser = metapropertyMapTraverser;
        }

        #endregion Constructors

        #region Methods

        public async Task Execute(Entity resourceEntity)
        {
            // parse setting map in dictionary
            var configuredMetaPropertyMap = SettingHelper.GetConfiguredMetaPropertyMapToBynder(InRiverContext.Settings, InRiverContext.Logger);
            if (configuredMetaPropertyMap == null)
            {
                InRiverContext.Log(LogLevel.Warning, "No metaproperty mapping configured, skipping metaproperty update");
                return;
            }

            string bynderUploadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
            if (bynderUploadStatus == BynderStates.Skipped)
            {
                return;
            }

            // block resourceEntity for bynder update if no BynderId is found on entity
            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId))
            {
                InRiverContext.Log(LogLevel.Warning, $"No BynderId found on resource {resourceEntity.Id}, skipping metaproperty update");
                return;
            }

            // only update bynder asset if resource has been downloaded or uploaded
            string bynderDownloadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState)?.Data;

            if ((string.IsNullOrWhiteSpace(bynderDownloadStatus) && string.IsNullOrWhiteSpace(bynderUploadStatus))
                || (bynderDownloadStatus != BynderStates.Done && bynderUploadStatus != BynderStates.Done))
            {
                InRiverContext.Log(LogLevel.Information, $"BynderId found on resource {resourceEntity.Id}, but resource not downloaded or uploaded yet, skipping metaproperty update");
                return;
            }

            // check if it may export
            if (!EntityAppliesToConditions(resourceEntity))
            {
                InRiverContext.Log(LogLevel.Information, $"Resource {resourceEntity.Id} does not apply to conditions, skipping metaproperty update");
                return;
            }

            // update metaproperties in Bynder
            var metapropertyValues = _metapropertyMapTraverser.GetMappedMetaPropertyValues(resourceEntity, configuredMetaPropertyMap);
            if (metapropertyValues.Count > 0)
            {
                // inform bynder of the changes:
                InRiverContext.Log(LogLevel.Information, $"Update metaproperties {string.Join("; ", metapropertyValues.Keys)}");

                var query = new ModifyMediaQuery(bynderId)
                {
                    MetapropertyOptions = metapropertyValues.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                    )
                };

                await _bynderClient.GetAssetService().ModifyMediaAsync(query);
            }
            else
            {
                // InRiverContext.Log(LogLevel.Verbose, $"No metaproperties mapped or found");
            }
        }

        #endregion Methods
    }
}