using Bynder.Api;
using Bynder.Names;
using Bynder.Utils.InRiver;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
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

            resourceEntity = _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);

            // block resourceEntity for bynder update if no BynderId is found on entity
            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // only update bynder asset if resource has been downloaded or uploaded
            string bynderDownloadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState)?.Data;
            string bynderUploadStatus = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
            if ((string.IsNullOrWhiteSpace(bynderDownloadStatus) && string.IsNullOrWhiteSpace(bynderUploadStatus))
                || (bynderDownloadStatus != BynderStates.Done && bynderUploadStatus != BynderStates.Done)) return;

            // parse setting map in dictionary
            var configuredMetaPropertyMap = GetConfiguredMetaPropertyMap();
            if (configuredMetaPropertyMap == null) return;

            // enrich metaproperties (metapropertyId => resourcefieldValue)
            var newMetapropertyValues = new Dictionary<string, List<string>>();
            AddMetapropertyValuesForEntity(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);
            AddMetapropertyValuesForLinks(resourceEntity, configuredMetaPropertyMap, newMetapropertyValues);

            if (newMetapropertyValues.Any())
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

        public Dictionary<string, string> GetConfiguredMetaPropertyMap()
        {
            if (_inRiverContext.Settings.ContainsKey(Config.Settings.MetapropertyMap))
            {
                return _inRiverContext.Settings[Config.Settings.MetapropertyMap]
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('='))
                    .ToDictionary(x => x[0], y => y[1]);
            }

            _inRiverContext.Logger.Log(LogLevel.Error, "Could not find configured metaproperty Map");
            return new Dictionary<string, string>();
        }

        private void AddMetapropertyValuesForEntity(Entity resourceEntity, Dictionary<string, string> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var metaProperty in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = resourceEntity.GetField(metaProperty.Value);
                var values = GetValuesForField(field);

                if (values.Count == 0)
                {
                    continue;
                }

                _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {metaProperty.Key} ({metaProperty.Value}) (R)");
                newMetapropertyValues.Add(metaProperty.Key, values);
            }
        }

        private void AddMetapropertyValuesForLinks(Entity resourceEntity, Dictionary<string, string> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            var inboundLinks =
                _inRiverContext.ExtensionManager.DataService.GetInboundLinksForEntity(resourceEntity.Id);

            // only process when inbound links are found
            if (inboundLinks.Count == 0)
            {
                return;
            }

            // skip resource metaproperties
            var nonResourceMetaProperties = configuredMetaPropertyMap.Where(x => !x.Value.StartsWith(EntityTypeIds.Resource));

            // iterate over configured metaproperties
            foreach (var metaProperty in nonResourceMetaProperties)
            {
                // save metaproperty values in a list so we can combine multiple occuneces to a single string
                var values = new List<string>();

                // check if configured fieldtype is on one of the inbound entities
                foreach (var inboundLink in inboundLinks)
                {
                    Field field = _inRiverContext.ExtensionManager.DataService.GetField(inboundLink.Source.Id, metaProperty.Value);
                    var fieldValues = GetValuesForField(field);
                    values.AddRange(fieldValues);
                }

                _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {metaProperty.Key} ({metaProperty.Value}) (L)");
                newMetapropertyValues.Add(metaProperty.Key, values);
            }
        }

        private List<string> GetValuesForField(Field field)
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

        #endregion Methods
    }
}