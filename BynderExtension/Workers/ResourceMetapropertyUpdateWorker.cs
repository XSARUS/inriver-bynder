using System;
using System.Collections.Generic;
using System.Linq;
using Bynder.Api;
using Bynder.Names;
using Bynder.Utils.InRiver;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    public class ResourceMetapropertyUpdateWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly IBynderClient _bynderClient;

        public ResourceMetapropertyUpdateWorker(inRiverContext inRiverContext, IBynderClient bynderClient)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        public void Execute(Entity resourceEntity)
        {
            // check if entity is resource and loadlevel is high enough
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeId.Resource)) return;

            resourceEntity = _inRiverContext.ExtensionManager.DataService.EntityLoadLevel(resourceEntity, LoadLevel.DataOnly);

            // block resourceEntity for bynder update if no BynderId is found on entity
            string bynderId = resourceEntity.GetField(FieldTypeId.ResourceBynderId).Data.ToString();
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // only update bynder asset if resource has status 'Done'
            string bynderStatus = resourceEntity.GetField(FieldTypeId.ResourceBynderDownloadState).Data.ToString();
            if (string.IsNullOrWhiteSpace(bynderStatus) || bynderStatus != BynderState.Done) return;

            // parse setting map in dictionary
            var configuredMetaPropertyMap = GetConfiguredMetaPropertyMap();
            if (configuredMetaPropertyMap == null) return;

            // enrich metaproperties (metapropertyId => resourcefieldValue)
            var newMetapropertyValues = new Dictionary<string, string>();
            foreach (var metaProperty in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = resourceEntity.GetField(metaProperty.Value);
                if (field != null)
                {
                    _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {metaProperty.Key} ({metaProperty.Value}) (R)");
                    newMetapropertyValues.Add(metaProperty.Key, field.Data?.ToString() ?? "");
                }
            }

            // next: process inbound links for metaproperties not directly mapped to resource fields.

            // get inbound links
            var inboundLinks =
                _inRiverContext.ExtensionManager.DataService.GetInboundLinksForEntity(resourceEntity.Id);

            // only process when inbound links are found
            if (inboundLinks.Count > 0)
            {
                // iterate over configured metaproperties
                foreach (var metaProperty in configuredMetaPropertyMap)
                {
                    // skip resource metaproperties
                    if(metaProperty.Value.StartsWith(EntityTypeId.Resource)) continue;

                    // save metaproperty values in a list so we can combine multiple occuneces to a single string
                    var values = new List<string>();

                    // check if configured fieldtype is on one of the inbound entities
                    foreach (var inboundLink in inboundLinks)
                    {
                        var fieldValue = _inRiverContext.ExtensionManager.DataService.GetFieldValue(inboundLink.Source.Id, metaProperty.Value);
                        if (fieldValue != null)
                        {
                            // if found, add to list
                            values.Add(fieldValue.ToString());
                        }
                    }

                    _inRiverContext.Logger.Log(LogLevel.Debug, $"Saving value for metaproperty {metaProperty.Key} ({metaProperty.Value}) (L)");
                    newMetapropertyValues.Add(metaProperty.Key, string.Join(",", values));
                }
            }

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
    }
}