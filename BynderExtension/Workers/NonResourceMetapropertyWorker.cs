using System.Linq;
using Bynder.Names;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    public class NonResourceMetapropertyWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly ResourceMetapropertyUpdateWorker _resourceMetapropertyUpdateWorker;

        public NonResourceMetapropertyWorker(inRiverContext inRiverContext, ResourceMetapropertyUpdateWorker resourceMetapropertyUpdateWorker)
        {
            _inRiverContext = inRiverContext;
            _resourceMetapropertyUpdateWorker = resourceMetapropertyUpdateWorker;
        }

        public void Execute(Entity entity, string[] fields)
        {
            if (entity.EntityType.Id == EntityTypeIds.Resource) return;

            // create metaproperty dictionary
            var metapropertyMap = _resourceMetapropertyUpdateWorker.GetConfiguredMetaPropertyMap();
            if (metapropertyMap.Count == 0) return;

            // check if any of the updated fields is in the mapping
            if (fields.Any(field => metapropertyMap.ContainsValue(field)))
            {
                var resourceIds = _inRiverContext.ExtensionManager.DataService.GetOutboundLinksForEntity(entity.Id)
                    .Where(l => l.LinkType.TargetEntityTypeId.Equals(EntityTypeIds.Resource))
                    .Select(l => l.Target.Id);

                foreach (var resourceId in resourceIds)
                {
                    var targetResourceEntity =
                        _inRiverContext.ExtensionManager.DataService.GetEntity(resourceId, LoadLevel.DataOnly);

                    // use the other worker for processing this resource
                    _resourceMetapropertyUpdateWorker.Execute(targetResourceEntity);
                }
            }
        }
    }
}
