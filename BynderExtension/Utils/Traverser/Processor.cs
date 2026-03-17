using Bynder.Models;
using inRiver.Remoting;
using inRiver.Remoting.Objects;
using inRiver.Remoting.Query;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bynder.Utils.Traverser
{
    public abstract class Processor
    {

        #region Properties

        protected IDataService DataService { get; }
        protected HashSet<int> EntityIdsToExport { get; set; }

        /// <summary>
        /// distinct by path and LinkTypeIdFromParent, which is unique for each entity and link
        /// </summary>
        protected Dictionary<string, int> SortOrderByPath { get; set; }

        protected IEnumerable<LinkType> SpecificationLinkTypes { get; set; }

        /// <summary>
        /// cache for run
        /// </summary>
        protected ConcurrentDictionary<int, string> UniqueValueCache { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// if you want to reuse the processor and don't call GetDataToExport. Then make sure you reset the cached values with ClearCache().
        /// </summary>
        /// <param name="specificationLinkTypes"></param>
        /// <param name="dataService"></param>
        protected Processor(IEnumerable<LinkType> specificationLinkTypes, IDataService dataService)
        {
            DataService = dataService;
            SpecificationLinkTypes = specificationLinkTypes;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Clear data of previous run(s)
        /// </summary>
        public virtual void ClearCache()
        {
            UniqueValueCache = new ConcurrentDictionary<int, string>();
            EntityIdsToExport = new HashSet<int>(256);
        }

        /// <summary>
        /// Traverses trough the <paramref name="configEntity"/> to gather entities and links which are related to the given start/root entity id.
        /// </summary>
        /// <param name="entityIds"></param>
        /// <param name="configEntity"></param>
        /// <param name="structureEntities"></param>
        /// <returns></returns>
        public virtual IDataGeneratorResult GetDataToExport(IEnumerable<int> entityIds, MetaPropertyMapTraverseConfig configEntity, IEnumerable<StructureEntity> structureEntities = null)
        {
            // Empty variables
            ClearCache();

            // Init input variables
            EntityIdsToExport.UnionWith(entityIds);
            List<Entity> entities = DataService.GetEntities(entityIds.ToList(), LoadLevel.Shallow);

            // cache for inbound links
            if (structureEntities != null)
            {
                SortOrderByPath = structureEntities
                    .GroupBy(x => $"{x.LinkTypeIdFromParent}_{x.Path}")
                    .ToDictionary(g => g.Key, g => g.Min(y => y.SortOrder));
            }

            // Get data according to config entity
            HandleConfigLinks(configEntity, entities, structureEntities, LinkDirection.InBound);
            HandleConfigLinks(configEntity, entities, structureEntities, LinkDirection.OutBound);

            List<ExportLink> linksToExport = ExportLinks.Distinct(new ExportLinkEqualityComparer()).ToList();

            // Handle links and specificationlinks
            IEnumerable<ExportLink> specificationLinksToExport = new List<ExportLink>(0);
            IEnumerable<ExportLink> filteredlinksToExport = linksToExport;
            HashSet<string> specificationLinkTypeIds = null;
            if (SpecificationLinkTypes != null && SpecificationLinkTypes.Any())
            {
                specificationLinkTypeIds = new HashSet<string>(SpecificationLinkTypes.Select(lt => lt.Id));
                specificationLinksToExport = linksToExport.AsParallel().Where(ol => specificationLinkTypeIds.Contains(ol.Type));
                filteredlinksToExport = linksToExport.AsParallel().Where(ol => !specificationLinkTypeIds.Contains(ol.Type));
            }

            // Get full entities
            var entitiesToExport = DataService.GetEntities(EntityIdsToExport.ToList(), LoadLevel.DataOnly);

            // Return result
            return new DataGeneratorResult
            {
                Entities = entitiesToExport,
                Links = filteredlinksToExport,
                SpecificationLinks = specificationLinksToExport
            };
        }

        /// <summary>
        /// Adds links to the total links list. TotalLinks has to be instantiated beforehand.
        /// </summary>
        /// <param name="linksToAdd"></param>
        protected void AddDataToTotalLinksList(IEnumerable<ExportLink> linksToAdd)
        {
            ExportLinks.AddRange(linksToAdd);
        }

        protected virtual void GetInboundLinksByConfigLink(int entityId, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities)
        {
            // No linktype to process
            if (configLink == null) return;

            // Get links
            IEnumerable<ExportLink> inboundLinks = GetInboundLinksForEntity(entityId, configLink, structureEntities, out bool useDataServiceOnOutbound);

            // No link found
            if (!inboundLinks.Any()) return;

            inboundLinks = inboundLinks.Distinct(new ExportLinkEqualityComparer());

            // Add links to list
            AddDataToTotalLinksList(inboundLinks);

            if (configLink.LinksOnly.Equals(false))
            {
                // Add to the entities list
                EntityIdsToExport.UnionWith(inboundLinks.Select(x => x.Source.Id));

                if (configLink.LinkEntities.Equals(true))
                {
                    // Add link entities to list
                    EntityIdsToExport.UnionWith(inboundLinks.AsParallel().Where(x => x.LinkEntityIdSpecified).Select(x => x.LinkEntityId));
                }
            }

            // Process the nested links
            ProcessNestedConfigLinksForInboundLinks(inboundLinks, configLink, structureEntities, useDataServiceOnOutbound);
        }

        protected override IEnumerable<ExportLink> GetInboundLinksForEntity(int entityId, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, out bool useDataServiceOnOutbound)
        {
            useDataServiceOnOutbound = false;

            return GetInboundLinksForEntityByDataService(entityId, configLink);
        }

        protected virtual IEnumerable<ExportLink> GetInboundLinksForEntityByDataService(int entityId, MetaPropertyMapTraverseConfig configLink)
        {
            var links = DataService.GetInboundLinksForEntityAndLinkType(entityId, configLink.LinkTypeId);

            // No link to process
            if (links.Count == 0) return new List<ExportLink>();

            // Null means: use all fieldsets
            if (configLink.FieldSet != null)
            {
                // get entities to check the fieldset
                var sourceIds = links.Select(x => x.Source.Id).ToList();
                var validSourceIds = GetValidEntities(sourceIds, configLink);
                var validSourceIdsHashSet = new HashSet<int>(validSourceIds);
                var filteredLinks = links.AsParallel().Where(x => validSourceIdsHashSet.Contains(x.Source.Id));

                return GetExportLinks(filteredLinks, configLink);
            }
            else
            {
                return GetExportLinks(links, configLink);
            }
        }

        protected virtual void GetOutboundLinksByConfigLink(int entityId, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceIfNotFound = false)
        {
            // No linktype to process
            if (configLink == null) return;

            // Get links
            IEnumerable<ExportLink> outboundLinks = GetOutboundLinksForEntity(entityId, configLink, structureEntities, useDataServiceIfNotFound);

            // No link found
            if (!outboundLinks.Any()) return;

            outboundLinks = outboundLinks.Distinct(new ExportLinkEqualityComparer());

            // Add links to list
            AddDataToTotalLinksList(outboundLinks);

            if (configLink.LinksOnly.Equals(false))
            {
                // Add to the entities list
                EntityIdsToExport.UnionWith(outboundLinks.Select(x => x.Target.Id));

                if (configLink.LinkEntities.Equals(true))
                {
                    // Add link entities to list
                    EntityIdsToExport.UnionWith(outboundLinks.AsParallel().Where(x => x.LinkEntityIdSpecified).Select(x => x.LinkEntityId));
                }
            }

            // Process the nested links
            ProcessNestedConfigLinksForOutboundLinks(outboundLinks, configLink, structureEntities, useDataServiceIfNotFound);
        }

        protected override IEnumerable<ExportLink> GetOutboundLinksForEntity(int entityId, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceIfNotFound)
        {
            return GetOutboundLinksForEntityByDataService(entityId, configLink);
        }
        protected virtual IEnumerable<ExportLink> GetOutboundLinksForEntityByDataService(int entityId, MetaPropertyMapTraverseConfig configLink)
        {
            var links = DataService.GetOutboundLinksForEntityAndLinkType(entityId, configLink.LinkTypeId);

            // No link to process
            if (links.Count == 0) return new List<ExportLink>();

            IEnumerable<ExportLink> validLinks;

            // Null means: use all fieldsets
            if (configLink.FieldSet != null)
            {
                var targetIds = links.Select(x => x.Target.Id).ToList();
                var validTargetIds = GetValidEntities(targetIds, configLink);
                validLinks = GetExportLinks(links.AsParallel().Where(x => validTargetIds.Contains(x.Target.Id)), configLink);
            }
            else
            {
                validLinks = GetExportLinks(links, configLink);
            }
            return validLinks;
        }

        protected virtual IEnumerable<ExportLink> GetOutboundLinksForEntityByStructureEntities(int entityId, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities)
        {
            // Could result in multiple found links, but we need to find them all because it could be that it has an other path, with other entities
            List<StructureEntity> outbounds = structureEntities.AsParallel().Where(e => e.LinkTypeIdFromParent.Equals(configLink.LinkTypeId) && e.ParentId == entityId).ToList();

            // No entities to process
            if (!outbounds.Any()) return new List<ExportLink>();

            IEnumerable<ExportLink> validLinks;

            // Null means: use all fieldsets
            if (configLink.FieldSet != null)
            {
                var targetIds = outbounds.Select(x => x.EntityId).ToList();
                var validTargetIds = GetValidEntities(targetIds, configLink);
                validLinks = GetExportLinks(outbounds.AsParallel().Where(x => validTargetIds.Contains(x.EntityId)), configLink);
            }
            else
            {
                validLinks = GetExportLinks(outbounds, configLink);
            }
            return validLinks;
        }

        protected string GetUniqueValue(int entityId, string uniqueField)
        {
            if (string.IsNullOrWhiteSpace(uniqueField))
            {
                return string.Empty;
            }

            if (UniqueValueCache.ContainsKey(entityId)) return UniqueValueCache[entityId];

            var value = DataService.GetFieldValue(entityId, uniqueField)?.ToString();
            UniqueValueCache.AddOrUpdate(entityId, value, (x, y) => value);

            return value;
        }

        protected virtual List<int> GetValidEntities(List<int> entityIds, MetaPropertyMapTraverseConfig configEntity)
        {
            // get entities to validate them
            var entities = DataService.GetEntities(entityIds, LoadLevel.Shallow);

            // Empty means: no fieldset set
            // filled means: use this fieldset
            return entities.AsParallel().Where(x => (x.FieldSetId ?? "").Equals(configEntity.FieldSet)).Select(x => x.Id).ToList();
        }
        protected void HandleConfigLinks(MetaPropertyMapTraverseConfig configEntity, List<Entity> entities, IEnumerable<StructureEntity> structureEntities, LinkDirection direction = LinkDirection.InBound)
        {
            // Get links of given direction on highest level
            List<T> configLinks = (direction == LinkDirection.OutBound) ? configEntity.Outbound : configEntity.Inbound;

            // No nested configs anymore in the config
            if (configLinks == null) return;

            foreach (MetaPropertyMapTraverseConfig configLink in configLinks)
            {
                foreach (var entity in entities)
                {
                    HandleConfigLinksForEntity(structureEntities, direction, configLink, entity);
                }
            }
        }

        protected void ProcessNestedConfigLinksForInboundLinks(IEnumerable<ExportLink> inboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceOnOutbound = false)
        {
            if (configLink.Inbound != null && configLink.Inbound.Any())
            {
                ProcessNestedInboundLinksForInboundLinks(inboundLinks, configLink, structureEntities);
            }
            if (configLink.Outbound != null && configLink.Outbound.Any())
            {
                ProcessNestedOutboundLinksForInboundLinks(inboundLinks, configLink, structureEntities, useDataServiceOnOutbound);
            }
        }

        protected void ProcessNestedConfigLinksForOutboundLinks(IEnumerable<ExportLink> outboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceIfNotFound)
        {
            if (configLink.Inbound != null && configLink.Inbound.Any())
            {
                ProcessNestedInboundLinksForOutboundLinks(outboundLinks, configLink, structureEntities);
            }

            if (configLink.Outbound != null && configLink.Outbound.Any())
            {
                ProcessNestedOutboundLinksForOutboundLinks(outboundLinks, configLink, structureEntities, useDataServiceIfNotFound);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="configLink"></param>
        /// <param name="sortOrder">Used to override the sort order. Otherwise it uses the sortorder from the given entity</param>
        /// <returns></returns>
        private ExportLink GetExportLinkByStructureEntity(StructureEntity entity, MetaPropertyMapTraverseConfig configLink, int? sortOrder = null)
        {
            return new ExportLink
            {
                Source = new ExportLinkNode
                {
                    Id = entity.ParentId,
                    UniqueFieldTypeId = configLink.SourceUniqueFieldTypeId,
                    Value = GetUniqueValue(entity.ParentId, configLink.SourceUniqueFieldTypeId)
                },

                Target = new ExportLinkNode
                {
                    Id = entity.EntityId,
                    UniqueFieldTypeId = configLink.TargetUniqueFieldTypeId,
                    Value = GetUniqueValue(entity.EntityId, configLink.TargetUniqueFieldTypeId)
                },
                Index = sortOrder ?? entity.SortOrder,
                Type = configLink.LinkTypeId,
                LinkEntityId = configLink.LinkEntities ? entity.LinkEntityId ?? 0 : 0
            };
        }

        private ExportLink GetExportLinkForLink(Link link, MetaPropertyMapTraverseConfig configLink)
        {
            return new ExportLink
            {
                Source = new ExportLinkNode
                {
                    Id = link.Source.Id,
                    UniqueFieldTypeId = configLink.SourceUniqueFieldTypeId,
                    Value = GetUniqueValue(link.Source.Id, configLink.SourceUniqueFieldTypeId)
                },
                Target = new ExportLinkNode
                {
                    Id = link.Target.Id,
                    UniqueFieldTypeId = configLink.TargetUniqueFieldTypeId,
                    Value = GetUniqueValue(link.Target.Id, configLink.TargetUniqueFieldTypeId)
                },
                Index = link.Index,
                Type = link.LinkType.Id,
                LinkEntityId = configLink.LinkEntities ? link.LinkEntity?.Id ?? 0 : 0
            };
        }

        /// <summary>
        /// Get export links for links
        /// </summary>
        /// <param name="links"></param>
        /// <param name="configLink"></param>
        /// <returns></returns>
        private IEnumerable<ExportLink> GetExportLinks(IEnumerable<Link> links, MetaPropertyMapTraverseConfig configLink)
        {
            var exportLinks = new ConcurrentBag<ExportLink>();

            Parallel.ForEach(links, new ParallelOptions { MaxDegreeOfParallelism = Generics.MaxConcurrentThreadCount }, (link) =>
            {
                exportLinks.Add(GetExportLinkForLink(link, configLink));
            });

            return exportLinks.ToArray();
        }

        /// <summary>
        /// Get export links for structure entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="configLink"></param>
        /// <returns></returns>
        private IEnumerable<ExportLink> GetExportLinks(IEnumerable<StructureEntity> entities, MetaPropertyMapTraverseConfig configLink)
        {
            var exportLinks = new ConcurrentBag<ExportLink>();

            Parallel.ForEach(entities, new ParallelOptions { MaxDegreeOfParallelism = Generics.MaxConcurrentThreadCount }, (entity) =>
            {
                exportLinks.Add(GetExportLinkByStructureEntity(entity, configLink));
            });

            return exportLinks.ToArray();
        }

        /// <summary>
        /// Get export links for structure entities with a custom sort order
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="configLink"></param>
        /// <returns></returns>
        private IEnumerable<ExportLink> GetExportLinks(IEnumerable<StructureEntityWithCustomSortOrder> entities, MetaPropertyMapTraverseConfig configLink)
        {
            var exportLinks = new ConcurrentBag<ExportLink>();

            Parallel.ForEach(entities, new ParallelOptions { MaxDegreeOfParallelism = Generics.MaxConcurrentThreadCount }, (entity) =>
            {
                exportLinks.Add(GetExportLinkByStructureEntity(entity.StructureEntity, configLink, entity.SortOrder));
            });

            return exportLinks.ToArray();
        }

        private void HandleConfigLinksForEntity(IEnumerable<StructureEntity> structureEntities, LinkDirection direction, MetaPropertyMapTraverseConfig configLink, Entity entity)
        {
            // Call the method for the given direction. Set totalLinks list of configlink.
            if (direction == LinkDirection.OutBound)
            {
                GetOutboundLinksByConfigLink(entity.Id, configLink, structureEntities);
            }
            else
            {
                GetInboundLinksByConfigLink(entity.Id, configLink, structureEntities);
            }
        }

        private void ProcessNestedInboundLinksForInboundLinks(IEnumerable<ExportLink> inboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities)
        {
            foreach (var inboundLink in configLink.Inbound)
            {
                foreach (var link in inboundLinks)
                {
                    GetInboundLinksByConfigLink(link.Source.Id, inboundLink, structureEntities);
                }
            }
        }

        private void ProcessNestedInboundLinksForOutboundLinks(IEnumerable<ExportLink> outboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities)
        {
            foreach (var inboundLink in configLink.Inbound)
            {
                foreach (var link in outboundLinks)
                {
                    GetInboundLinksByConfigLink(link.Target.Id, inboundLink, structureEntities);
                }
            }
        }

        private void ProcessNestedOutboundLinksForInboundLinks(IEnumerable<ExportLink> inboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceOnOutbound)
        {
            foreach (var outboundLink in configLink.Outbound)
            {
                foreach (var link in inboundLinks)
                {
                    GetOutboundLinksByConfigLink(link.Source.Id, outboundLink, structureEntities, useDataServiceOnOutbound);
                }
            }
        }

        private void ProcessNestedOutboundLinksForOutboundLinks(IEnumerable<ExportLink> outboundLinks, MetaPropertyMapTraverseConfig configLink, IEnumerable<StructureEntity> structureEntities, bool useDataServiceIfNotFound)
        {
            foreach (var outboundLink in configLink.Outbound)
            {
                foreach (var link in outboundLinks)
                {
                    GetOutboundLinksByConfigLink(link.Target.Id, outboundLink, structureEntities, useDataServiceIfNotFound);
                }
            }
        }

        #endregion Methods

    }
}
