using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using Api;
    using inRiver.Remoting;
    using Models;
    using Names;
    using Newtonsoft.Json;
    using Utils.Helpers;
    using Utils.InRiver;

    public class ResourceMetapropertyUpdateWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public ResourceMetapropertyUpdateWorker(inRiverContext inRiverContext, IBynderClient bynderClient)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            // check if entity is resource
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;

            // parse setting map in dictionary
            var configuredMetaPropertyMap = SettingHelper.GetConfiguredMetaPropertyMap(_inRiverContext.Settings, _inRiverContext.Logger);
            if (configuredMetaPropertyMap.Count == 0)
            {
                _inRiverContext.Log(LogLevel.Warning, "No metaproperty mappings configured, skipping metaproperty update");
                return;
            }

            // get full resource entity (again to also prevent revision errors)
            resourceEntity = _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);

            // block resourceEntity for bynder update if no BynderId is found on entity
            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId))
            {
                _inRiverContext.Log(LogLevel.Warning, $"No BynderId found on resource {resourceEntity.Id}, skipping metaproperty update");
                return; 
            }

            // only update bynder asset if resource has been downloaded or uploaded
            string bynderDownloadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState)?.Data;
            string bynderUploadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
            if ((string.IsNullOrWhiteSpace(bynderDownloadStatus) && string.IsNullOrWhiteSpace(bynderUploadStatus))
                || (bynderDownloadStatus != BynderStates.Done && bynderUploadStatus != BynderStates.Done))
            {
                _inRiverContext.Log(LogLevel.Information, $"BynderId found on resource {resourceEntity.Id}, but resource not downloaded or uploaded yet, skipping metaproperty update");
                return;
            }

            // check if it may export
            if (!EntityAppliesToConditions(resourceEntity)) 
            { 
                _inRiverContext.Log(LogLevel.Information, $"Resource {resourceEntity.Id} does not apply to conditions, skipping metaproperty update");
                return;
            }

            // enrich metaproperties (metapropertyId => resourcefieldValue)
            var newMetapropertyValues = new Dictionary<string, List<string>>();
            AddMetapropertyValuesForEntity(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);
            AddMetapropertyValuesForLinks(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);
            FilterMetapropertyValues(configuredMetaPropertyMap, newMetapropertyValues);

            if (newMetapropertyValues.Count > 0)
            {
                // inform bynder of the changes:
                _inRiverContext.Log(LogLevel.Information, $"Update metaproperties {string.Join(";", newMetapropertyValues)}");
                _bynderClient.SetMetaProperties(bynderId, newMetapropertyValues);
            }
            else
            {
                _inRiverContext.Log(LogLevel.Verbose, $"No metaproperties mapped or found");
            }
        }

        protected static void FilterMetapropertyValues(List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                if (!newMetapropertyValues.ContainsKey(map.BynderMetaProperty)) continue;

                var values = newMetapropertyValues[map.BynderMetaProperty];
                if (!map.IsMultiValue && values.Count > 1)
                {
                    newMetapropertyValues[map.BynderMetaProperty] = new List<string> { values[0] };
                }
            }
        }

        protected static List<string> GetValuesForField(Field field, inRiverContext inRiverContext)
        {
            var values = new List<string>();

            if (field == null || string.IsNullOrWhiteSpace(field?.Data?.ToString()))
            {
                return values;
            }

            if (field.FieldType.DataType.Equals(DataType.LocaleString))
            {
                Dictionary<string, string> valuePairs = new Dictionary<string, string>();
                LocaleString localeString = (LocaleString)field.Data as LocaleString;
                if (localeString != null)
                {
                    Dictionary<string, string> localeMappings = SettingHelper.GetLocaleMappings(inRiverContext.Settings, inRiverContext.Logger);
                    foreach (var lang in localeString.Languages)
                    {
                        if (localeMappings.ContainsKey(lang.Name))
                        {
                            valuePairs.Add(localeMappings[lang.Name], localeString[lang]);
                        }
                    }
                }

                values.Add(JsonConvert.SerializeObject(valuePairs));
            }
            else if (field.FieldType.DataType.Equals(DataType.CVL))
            {
                CVL cvl = inRiverContext.ExtensionManager.ModelService.GetCVL(field.FieldType.CVLId);
                if (cvl.DataType.Equals(DataType.LocaleString))
                {
                    Dictionary<string, string> localeMappings = SettingHelper.GetLocaleMappings(inRiverContext.Settings, inRiverContext.Logger);
                    IEnumerable<string> keys = field.Data.ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct();
                    List<CVLValue> cvlValues = inRiverContext.ExtensionManager.ModelService.GetCVLValuesForCVL(cvl.Id);

                    foreach (var key in keys)
                    {
                        CVLValue cvlValue = cvlValues.FirstOrDefault(c => c.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
                        if (cvlValue == null) continue;
                        LocaleString localeString = (LocaleString)cvlValue.Value as LocaleString;
                        if (localeString == null) continue;

                        Dictionary<string, string> valuePairs = new Dictionary<string, string>();

                        foreach (var lang in localeString.Languages)
                        {
                            if (localeMappings.ContainsKey(lang.Name))
                            {
                                valuePairs.Add(localeMappings[lang.Name], localeString[lang]);
                            }
                        }

                        values.Add(JsonConvert.SerializeObject(valuePairs));
                    }
                }
                else if (field.FieldType.Multivalue)
                {
                    string[] keys = field.Data.ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
                    if (keys.Any())
                    {
                        values.AddRange(keys);
                    }
                }
            }
            else
            {
                values.Add(field.Data.ToString());
            }

            return values;
        }

        protected void AddMetapropertyValuesForEntity(Entity resourceEntity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = resourceEntity.GetField(map.InriverFieldTypeId);
                var values = GetValuesForField(field, _inRiverContext);

                _inRiverContext.Log(LogLevel.Debug, $"Checking value(s) for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}): {values.Count} values");

                if (values.Count == 0)
                {
                    continue;
                }

                _inRiverContext.Log(LogLevel.Debug, $"Saving value for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}) (R)");
                newMetapropertyValues.Add(map.BynderMetaProperty, values);
            }
        }

        protected void AddMetapropertyValuesForLinks(Entity resourceEntity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            var inboundLinks =
                _inRiverContext.ExtensionManager.DataService.GetInboundLinksForEntity(resourceEntity.Id);

            // only process when inbound links are found
            if (inboundLinks.Count == 0)
            {
                return;
            }

            // skip resource metaproperties
            var filteredMapping = configuredMetaPropertyMap.Where(x => !x.InriverFieldTypeId.StartsWith(EntityTypeIds.Resource));

            // iterate over configured metaproperties
            foreach (var mapping in filteredMapping)
            {
                // save metaproperty values in a list so we can combine multiple occurences to a single string
                var values = new List<string>();

                // check if configured fieldtype is on one of the inbound entities
                foreach (var inboundLink in inboundLinks)
                {
                    Field field = _inRiverContext.ExtensionManager.DataService.GetField(inboundLink.Source.Id, mapping.InriverFieldTypeId);
                    var fieldValues = GetValuesForField(field, _inRiverContext);
                    values.AddRange(fieldValues);
                }

                _inRiverContext.Log(LogLevel.Debug, $"Saving value for metaproperty {mapping.BynderMetaProperty} ({mapping.InriverFieldTypeId}) (L)");
                newMetapropertyValues.Add(mapping.BynderMetaProperty, values);
            }
        }

        private static bool GetConditionResult(Entity entity, ExportCondition condition, inRiverContext inriverContext)
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

            List<string> fieldValues = GetValuesForField(field, inriverContext);

            return ConditionHelper.ValuesApplyToCondition(fieldValues, condition);
        }

        private bool EntityAppliesToConditions(Entity entity)
        {
            var conditions = SettingHelper.GetExportConditions(_inRiverContext.Settings, _inRiverContext.Logger);

            // return true if no conditions found. Conditions are optional.
            if (conditions.Count == 0) return true;

            foreach (var condition in conditions)
            {
                if (!GetConditionResult(entity, condition, _inRiverContext)) return false;
            }

            return true;
        }

        #endregion Methods
    }
}