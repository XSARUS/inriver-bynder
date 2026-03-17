using Bynder.Models;
using Bynder.Names;
using Bynder.Utils.Extensions;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Traverser
{
    public class MetaPropertyTraverser
    {
        private inRiverContext _context;

        public MetaPropertyTraverser(inRiverContext context)
        {
            _context = context;
        }

        public Dictionary<string, List<string>> GetMappedMetaPropertyValues(MetaPropertyMapTraverseConfig config)
        {
            var result = new Dictionary<string, List<string>>();
            //todo those protected methods are old, just copied them over from the worker. TODO implement traversal code
            return result;
        }


        protected void AddMetapropertyValuesForEntity(Entity resourceEntity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = resourceEntity.GetField(map.InriverFieldTypeId);
                var values = GetValuesForField(field);

                _context.Log(LogLevel.Debug, $"Checking value(s) for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}): {values.Count} values");

                if (values.Count == 0)
                {
                    continue;
                }

                _context.Log(LogLevel.Debug, $"Saving value for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}) (R)");
                newMetapropertyValues.Add(map.BynderMetaProperty, values);
            }
        }

        protected void AddMetapropertyValuesForLinks(Entity resourceEntity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            var inboundLinks =
                _context.ExtensionManager.DataService.GetInboundLinksForEntity(resourceEntity.Id);

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
                    Field field = _context.ExtensionManager.DataService.GetField(inboundLink.Source.Id, mapping.InriverFieldTypeId);
                    var fieldValues = GetValuesForField(field);
                    values.AddRange(fieldValues);
                }

                _context.Log(LogLevel.Debug, $"Saving value for metaproperty {mapping.BynderMetaProperty} ({mapping.InriverFieldTypeId}) (L)");
                newMetapropertyValues.Add(mapping.BynderMetaProperty, values);
            }
        }

        protected static void FilterMetapropertyValues(List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                if (!newMetapropertyValues.ContainsKey(map.BynderMetaProperty)) continue;

                // if the bynder property is not multivalue but we have multiple values then only grab the first
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
                var keys = field.Data.ToString().ToIEnumerable<string>(';');
                if (keys.Any())
                {
                    values.AddRange(keys);
                }
            }
            else
            {
                // should everything be string?
                values.Add(field.Data.ToString());
            }

            return values;
        }
    }
}
