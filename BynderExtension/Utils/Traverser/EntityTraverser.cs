using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using inRiver.Remoting.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Traverser
{
    using Models;

    public class EntityTraverser
    {
        private readonly inRiverContext _context;

        public EntityTraverser(inRiverContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns start entity IDs based on the config root.
        /// Traverses upward using config-structure.
        /// </summary>
        public List<int> GetStartEntityIds(Entity entity, MetaPropertyMapTraverseConfig config)
        {
            if (entity == null || config == null)
                return new List<int>();

            // If entity already matches config root
            if (MatchesEntityNode(entity, config))
                return new List<int> { entity.Id };

            // Assign traversal metadata
            int idCounter = 1;
            AddTraversalMetadata(config, ref idCounter, 0, null);

            // Flatten config to dictionary (id->node)
            var flat = Flatten(config).ToDictionary(n => n.Id);

            // Find config nodes that match this entity
            var matchingNodes = new List<MetaPropertyMapTraverseConfig>();
            foreach (var n in flat.Values)
            {
                if (MatchesEntityNode(entity, n))
                    matchingNodes.Add(n);
            }

            if (matchingNodes.Count == 0)
                return new List<int>();

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<int>();

            foreach (var n in matchingNodes)
            {
                TraverseUpwards(entity.Id, n, flat, visited, results);
            }

            // Deduplicate
            return results.Distinct().ToList();
        }


        public List<int> GetStartEntityIds(int entityId, MetaPropertyMapTraverseConfig config)
        {
            var entity = _context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.Shallow);
            return GetStartEntityIds(entity, config);
        }


        // -------------------------------------------------------------------
        // TRAVERSAL CORE (Upward)
        // -------------------------------------------------------------------

        private void TraverseUpwards(
            int entityId,
            MetaPropertyMapTraverseConfig currentNode,
            Dictionary<int, MetaPropertyMapTraverseConfig> flat,
            HashSet<string> visited,
            List<int> results)
        {
            string visitKey = entityId + ":" + currentNode.Id;
            if (visited.Contains(visitKey))
                return;
            visited.Add(visitKey);

            // Root node reached
            if (currentNode.ParentId == 0)
            {
                results.Add(entityId);
                return;
            }

            // Lookup parent node
            MetaPropertyMapTraverseConfig parentNode;
            if (!flat.TryGetValue(currentNode.ParentId, out parentNode))
                return;

            // Determine linktype
            string linkType = currentNode.LinkTypeId;
            if (string.IsNullOrWhiteSpace(linkType))
                return;

            // Determine direction
            List<int> nextEntityIds = new List<int>();
            if (currentNode.LinkDirectionToParent == LinkDirection.InBound)
            {
                // runtime inbound
                nextEntityIds = GetInboundParents(entityId, linkType);
            }
            else if (currentNode.LinkDirectionToParent == LinkDirection.OutBound)
            {
                // runtime outbound
                nextEntityIds = GetOutboundParents(entityId, linkType);
            }

            foreach (var nextId in nextEntityIds)
            {
                TraverseUpwards(nextId, parentNode, flat, visited, results);
            }
        }


        // -------------------------------------------------------------------
        // RUNTIME LINK HELPERS
        // -------------------------------------------------------------------

        private List<int> GetInboundParents(int entityId, string linkTypeId)
        {
            var links =
                _context.ExtensionManager.DataService.GetInboundLinksForEntityAndLinkType(entityId, linkTypeId);

            var result = new List<int>();
            foreach (var l in links)
            {
                if (!result.Contains(l.Source.Id))
                    result.Add(l.Source.Id);
            }
            return result;
        }


        private List<int> GetOutboundParents(int entityId, string linkTypeId)
        {
            var links =
                _context.ExtensionManager.DataService.GetOutboundLinksForEntityAndLinkType(entityId, linkTypeId);

            var result = new List<int>();
            foreach (var l in links)
            {
                if (!result.Contains(l.Target.Id))
                    result.Add(l.Target.Id);
            }
            return result;
        }


        // -------------------------------------------------------------------
        // CONFIG METADATA HELPERS
        // -------------------------------------------------------------------

        private void AddTraversalMetadata(
            MetaPropertyMapTraverseConfig node,
            ref int idCounter,
            int parentId,
            LinkDirection? directionToParent)
        {
            node.Id = idCounter++;
            node.ParentId = parentId;
            node.LinkDirectionToParent = directionToParent;

            // inbound in config = outbound at runtime → parent direction: InBound
            foreach (var child in node.Inbound)
            {
                AddTraversalMetadata(child, ref idCounter, node.Id, LinkDirection.InBound);
            }

            // outbound in config = inbound at runtime → parent direction: OutBound
            foreach (var child in node.Outbound)
            {
                AddTraversalMetadata(child, ref idCounter, node.Id, LinkDirection.OutBound);
            }
        }


        private IEnumerable<MetaPropertyMapTraverseConfig> Flatten(MetaPropertyMapTraverseConfig root)
        {
            yield return root;

            foreach (var c in root.Inbound)
            {
                foreach (var n in Flatten(c))
                    yield return n;
            }

            foreach (var c in root.Outbound)
            {
                foreach (var n in Flatten(c))
                    yield return n;
            }
        }


        private bool MatchesEntityNode(Entity entity, MetaPropertyMapTraverseConfig node)
        {
            if (!entity.EntityType.Id.Equals(node.EntityTypeId, StringComparison.OrdinalIgnoreCase))
                return false;

            // null = all fieldsets allowed
            if (node.FieldSet == null)
                return true;

            var fs = entity.FieldSetId ?? string.Empty;
            return fs.Equals(node.FieldSet, StringComparison.OrdinalIgnoreCase);
        }
    }
}