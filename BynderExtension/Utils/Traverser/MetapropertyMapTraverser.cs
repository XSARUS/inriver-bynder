using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Traverser
{
    using Extensions;
    using Models;

    public class MetapropertyMapTraverser
    {
        private readonly inRiverContext _context;

        public MetapropertyMapTraverser(inRiverContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Collects mapped Bynder metaproperty values for a given start entity (typically a Resource)
        /// using the traversal config tree.
        /// </summary>
        public Dictionary<string, List<string>> GetMappedMetaPropertyValues(Entity startEntity, MetaPropertyMapTraverseConfig config)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (startEntity == null || config == null)
                return result;

            // Track visited (entityId + configHash) to avoid loops
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            TraverseNode(startEntity, config, result, visited);

            // Apply multivalue filtering based on the maps encountered at root + nested nodes.
            // We can’t easily know "all configured maps" unless we flatten config, so do that once:
            var allMaps = FlattenConfig(config)
                .Where(n => n.MetaPropertyMapping != null && n.MetaPropertyMapping.Any())
                .SelectMany(n => n.MetaPropertyMapping)
                .ToList();

            EnforceSingleValueMetaProperties(allMaps, result);

            // Remove duplicate values
            foreach (var key in result.Keys.ToList())
            {
                result[key] = result[key]
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Convenience overload when you only have an entity id.
        /// </summary>
        public Dictionary<string, List<string>> GetMappedMetaPropertyValues(int startEntityId, MetaPropertyMapTraverseConfig config)
        {
            var entity = _context.ExtensionManager.DataService.GetEntity(startEntityId, LoadLevel.DataOnly);
            return GetMappedMetaPropertyValues(entity, config);
        }

        private void TraverseNode(
            Entity currentEntity,
            MetaPropertyMapTraverseConfig node,
            Dictionary<string, List<string>> result,
            HashSet<string> visited)
        {
            if (currentEntity == null || node == null)
                return;

            // Validate entity type and fieldset if configured
            if (!Applies(currentEntity, node))
                return;

            var visitKey = $"{currentEntity.Id}:{node.Hash()}";
            if (!visited.Add(visitKey))
                return;

            // Collect metaproperty values from the current entity itself (fields)
            if (node.MetaPropertyMapping != null && node.MetaPropertyMapping.Any())
            {
                AddOrMergeEntityValues(currentEntity, node.MetaPropertyMapping, result);
            }

            // Traverse inbound children
            if (node.Inbound != null && node.Inbound.Any())
            {
                foreach (var child in node.Inbound)
                {
                    // child.LinkTypeId describes the link type between current and next
                    foreach (var next in GetInboundEntities(currentEntity.Id, child.LinkTypeId))
                    {
                        TraverseNode(next, child, result, visited);
                    }
                }
            }

            // Traverse outbound children
            if (node.Outbound != null && node.Outbound.Any())
            {
                foreach (var child in node.Outbound)
                {
                    foreach (var next in GetOutboundEntities(currentEntity.Id, child.LinkTypeId))
                    {
                        TraverseNode(next, child, result, visited);
                    }
                }
            }
        }

        private bool Applies(Entity entity, MetaPropertyMapTraverseConfig node)
        {
            // EntityTypeId is required in config; match if set
            if (!string.IsNullOrWhiteSpace(node.EntityTypeId) &&
                !entity.EntityType.Id.Equals(node.EntityTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // null is all fieldsets, empty is no fieldset, filled is a specific fieldset
            if (node.FieldSet != null)
            {
                var entityFieldSet = entity.FieldSetId ?? string.Empty;
                if (!entityFieldSet.Equals(node.FieldSet, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private IEnumerable<Entity> GetInboundEntities(int entityId, string linkTypeId)
        {
            if (string.IsNullOrWhiteSpace(linkTypeId))
                return Enumerable.Empty<Entity>();

            var links = _context.ExtensionManager.DataService.GetInboundLinksForEntityAndLinkType(entityId, linkTypeId);
            if (links == null || links.Count == 0)
                return Enumerable.Empty<Entity>();

            var ids = links.Select(l => l.Source.Id).Distinct().ToList();
            if (ids.Count == 0)
                return Enumerable.Empty<Entity>();

            return _context.ExtensionManager.DataService.GetEntities(ids, LoadLevel.DataOnly);
        }

        private IEnumerable<Entity> GetOutboundEntities(int entityId, string linkTypeId)
        {
            if (string.IsNullOrWhiteSpace(linkTypeId))
                return Enumerable.Empty<Entity>();

            var links = _context.ExtensionManager.DataService.GetOutboundLinksForEntityAndLinkType(entityId, linkTypeId);
            if (links == null || links.Count == 0)
                return Enumerable.Empty<Entity>();

            var ids = links.Select(l => l.Target.Id).Distinct().ToList();
            if (ids.Count == 0)
                return Enumerable.Empty<Entity>();

            return _context.ExtensionManager.DataService.GetEntities(ids, LoadLevel.DataOnly);
        }

        private void AddOrMergeEntityValues(
            Entity entity,
            List<MetaPropertyMap> maps,
            Dictionary<string, List<string>> result)
        {
            // Reuse your existing logic but merge into result instead of throwing on duplicate keys
            var newValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            AddMetapropertyValuesForEntity(entity, maps, newValues);

            Merge(result, newValues);
        }

        private static void Merge(
            Dictionary<string, List<string>> target,
            Dictionary<string, List<string>> incoming)
        {
            foreach (var incomingKvp in incoming)
            {
                if (!target.TryGetValue(incomingKvp.Key, out var existing))
                {
                    target[incomingKvp.Key] = incomingKvp.Value ?? new List<string>();
                    continue;
                }

                if (incomingKvp.Value == null || incomingKvp.Value.Count == 0)
                    continue;

                existing.AddRange(incomingKvp.Value);
            }
        }

        private static IEnumerable<MetaPropertyMapTraverseConfig> FlattenConfig(MetaPropertyMapTraverseConfig root)
        {
            if (root == null) yield break;

            yield return root;

            if (root.Inbound != null)
            {
                foreach (var c in root.Inbound.SelectMany(FlattenConfig))
                    yield return c;
            }

            if (root.Outbound != null)
            {
                foreach (var c in root.Outbound.SelectMany(FlattenConfig))
                    yield return c;
            }
        }

        protected void AddMetapropertyValuesForEntity(Entity entity, List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
        {
            foreach (var map in configuredMetaPropertyMap)
            {
                // check if configured fieldtype is on entity
                var field = entity.GetField(map.InriverFieldTypeId);
                var values = GetValuesForField(field);

                _context.Log(LogLevel.Debug, $"Checking value(s) for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}): {values.Count} values");

                if (values.Count == 0)
                {
                    continue;
                }

                _context.Log(LogLevel.Debug, $"Saving value for metaproperty {map.BynderMetaProperty} ({map.InriverFieldTypeId}) (R)");

                // update existing or add new
                if (!newMetapropertyValues.TryGetValue(map.BynderMetaProperty, out var list))
                {
                    newMetapropertyValues[map.BynderMetaProperty] = values;
                }
                else
                {
                    list.AddRange(values);
                }
            }
        }

        /// <summary>
        /// Ensures that metaproperties configured as single-value contain at most one value.
        /// Extra values are discarded.
        /// </summary>
        protected static void EnforceSingleValueMetaProperties(List<MetaPropertyMap> configuredMetaPropertyMap, Dictionary<string, List<string>> newMetapropertyValues)
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
                values.Add(field.Data.ToString());
            }

            return values;
        }
    }
}
