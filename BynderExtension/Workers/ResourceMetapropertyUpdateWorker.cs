using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;

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
    /// Updates metaproperties on asses in Bynder if Entity applies to EXPORT_CONDITIONS
    /// </summary>
    public class ResourceMetapropertyUpdateWorker : AbstractBynderWorker, IWorker
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings => ResourceMetapropertyUpdateWorkerSettingsProvider.Create();
        private MetapropertyMapTraverser _metapropertyMapTraverser;

        #endregion Properties

        #region Constructors

        public ResourceMetapropertyUpdateWorker(MetapropertyMapTraverser metapropertyMapTraverser, inRiverContext inRiverContext, IBynderClient bynderClient = null) : base(inRiverContext, bynderClient)
        {
            _metapropertyMapTraverser = metapropertyMapTraverser;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            // parse setting map in dictionary
            var configuredMetaPropertyMap = SettingHelper.GetConfiguredMetaPropertyMapToBynder(InRiverContext.Settings, InRiverContext.Logger);
            if (configuredMetaPropertyMap == null)
            {
                InRiverContext.Log(LogLevel.Warning, "No metaproperty mapping configured, skipping metaproperty update");
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
            string bynderUploadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;

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
                InRiverContext.Log(LogLevel.Information, $"Update metaproperties {string.Join(";", metapropertyValues.Keys)}");

                var query = new ModifyMediaQuery(bynderId)
                {
                    MetapropertyOptions = metapropertyValues.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (IList<string>)kvp.Value
                    )
                };

                _bynderClient.GetAssetService().ModifyMediaAsync(query).GetAwaiter().GetResult();
            }
            else
            {
                InRiverContext.Log(LogLevel.Verbose, $"No metaproperties mapped or found");
            }
        }

        protected static List<string> GetValuesForField(Field field)
        {
            var values = new List<string>();

            if (field == null || string.IsNullOrWhiteSpace(field?.Data?.ToString()))
            {
                return values;
            }

            if (field.FieldType.DataType.Equals(DataType.CVL) && field.FieldType.Multivalue)
            {
                var keys = field.Data.ToString().ToIEnumerable<string>(';');
                if (keys.Any())
                {
                    values.AddRange(keys);
                }
            }
            else
            {
                values.Add(field.Data.ToString());
            }

            return values;
        }
      
        private static bool GetConditionResult(Entity entity, ExportCondition condition)
        {
            var field = entity.GetField(condition.InRiverFieldTypeId);

            // metaproperty is not included in asset, when the value is null
            if (field == null || field.IsEmpty())
            {
                // check if there are conditions or if the only condition value is null
                if (condition.Values.Count == 0 || (condition.Values.Count == 1 && string.IsNullOrEmpty(condition.Values[0]))) return true;

                // return false, because the metaproperty does not have a value, but the condition does
                return false;
            }

            List<string> fieldValues = GetValuesForField(field);

            return ConditionHelper.ValuesApplyToCondition(fieldValues, condition);
        }

        private bool EntityAppliesToConditions(Entity entity)
        {
            var conditions = SettingHelper.GetExportConditions(InRiverContext.Settings, InRiverContext.Logger);

            // return true if no conditions found. Conditions are optional.
            if (conditions.Count == 0) return true;

            foreach (var condition in conditions)
            {
                if (!GetConditionResult(entity, condition))
                {
                    InRiverContext.Log(LogLevel.Debug, $"Resource {entity.Id} does not apply to condition on field {condition.InRiverFieldTypeId} [value: {entity.GetField(condition.InRiverFieldTypeId).Data?.ToString()}], skipping metaproperty update; Condition values: {string.Join(";", condition.Values)}");
                    return false;
                }
            }

            return true;
        }

        #endregion Methods
    }
}