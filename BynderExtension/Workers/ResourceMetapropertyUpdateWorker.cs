using Bynder.Api;
using Bynder.Models;
using Bynder.Names;
using Bynder.Utils.Extensions;
using Bynder.Utils.InRiver;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
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
            var configuredMetaPropertyMap = GetConfiguredMetaPropertyMap();
            if (configuredMetaPropertyMap.Count == 0) return;

            // get full resource entity (again to also prevent revision errors)
            resourceEntity = _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);

            // block resourceEntity for bynder update if no BynderId is found on entity
            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // only update bynder asset if resource has been downloaded or uploaded
            string bynderDownloadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState)?.Data;
            string bynderUploadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
            if ((string.IsNullOrWhiteSpace(bynderDownloadStatus) && string.IsNullOrWhiteSpace(bynderUploadStatus))
                || (bynderDownloadStatus != BynderStates.Done && bynderUploadStatus != BynderStates.Done)) return;

            // enrich metaproperties (metapropertyId => resourcefieldValue)
            var newMetapropertyValues = new Dictionary<string, List<string>>();
            AddMetapropertyValuesForEntity(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);
            AddMetapropertyValuesForLinks(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);
            FilterMetapropertyValues(configuredMetaPropertyMap, newMetapropertyValues);

            if (newMetapropertyValues.Count > 0)
            {
                // inform bynder of the changes:
                _inRiverContext.Logger.Log(LogLevel.Information, $"Update metaproperties {string.Join(";", newMetapropertyValues)}");
                _bynderClient.SetMetaProperties(bynderId, newMetapropertyValues);
            }
            else
            {
                _inRiverContext.Logger.Log(LogLevel.Verbose, $"No metaproperties mapped or found");
            }
        }

        public List<MetaPropertyMap> GetConfiguredMetaPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Config.Settings.MetapropertyMap))
            {
                var settingValue = _inRiverContext.Settings[Config.Settings.MetapropertyMap];

                if (string.IsNullOrEmpty(settingValue))
                {
                    return new List<MetaPropertyMap>();
                }

                settingValue = settingValue.Trim();
                if (settingValue.StartsWith("[") && settingValue.EndsWith("]"))
                {
                    return JsonConvert.DeserializeObject<List<MetaPropertyMap>>(settingValue)
                        .Where(map => !string.IsNullOrEmpty(map.BynderMetaProperty) && !string.IsNullOrEmpty(map.InriverFieldTypeId))
                        .ToList();
                }

                // support old format for backwards compatiblity
                var mapDict = _inRiverContext.Settings[Config.Settings.MetapropertyMap].ToDictionary<string, string>(',', '=');
                return mapDict
                    .Select(x => new MetaPropertyMap { BynderMetaProperty = x.Key, InriverFieldTypeId = x.Value, IsMultiValue = true })
                    .Where(map => !string.IsNullOrEmpty(map.BynderMetaProperty) && !string.IsNullOrEmpty(map.InriverFieldTypeId))
                    .ToList();
            }

            _inRiverContext.Logger.Log(LogLevel.Verbose, "Could not find configured metaproperty Map");
            return new List<MetaPropertyMap>();
        }

        protected static void FilterMetapropertyValues(List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach(var map in configuredMetaPropertyMap)
            {
                if (!newMetapropertyValues.ContainsKey(map.BynderMetaProperty)) continue;

                var values = newMetapropertyValues[map.BynderMetaProperty];
                if (!map.IsMultiValue && values.Count > 1)
                {
                    newMetapropertyValues[map.BynderMetaProperty] = new List<string> { values[0] };
                }
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
                string[] keys = field.Data.ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
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

        protected void AddMetapropertyValuesForEntity(Entity resourceEntity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = resourceEntity.GetField(map.InriverFieldTypeId);
                var values = GetValuesForField(field);

                if (values.Count == 0)
                {
                    continue;
                }

                _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}) (R)");
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
                    var fieldValues = GetValuesForField(field);
                    values.AddRange(fieldValues);
                }

                _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {mapping.BynderMetaProperty} ({mapping.InriverFieldTypeId}) (L)");
                newMetapropertyValues.Add(mapping.BynderMetaProperty, values);
            }
        }

        #endregion Methods
    }
}