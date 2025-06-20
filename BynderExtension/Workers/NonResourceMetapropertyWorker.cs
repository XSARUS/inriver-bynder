using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System.Linq;

namespace Bynder.Workers
{
    using Names;
    using Utils.Helpers;

    public class NonResourceMetapropertyWorker : IWorker
    {
        #region Fields

        private readonly inRiverContext _inRiverContext;
        private readonly ResourceMetapropertyUpdateWorker _resourceMetapropertyUpdateWorker;

        #endregion Fields

        #region Constructors

        public NonResourceMetapropertyWorker(inRiverContext inRiverContext, ResourceMetapropertyUpdateWorker resourceMetapropertyUpdateWorker)
        {
            _inRiverContext = inRiverContext;
            _resourceMetapropertyUpdateWorker = resourceMetapropertyUpdateWorker;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity entity, string[] fields)
        {
            if (entity.EntityType.Id == EntityTypeIds.Resource) return;

            // create metaproperty dictionary
            var metapropertyMap = SettingHelper.GetConfiguredMetaPropertyMap(_inRiverContext.Settings, _inRiverContext.Logger);
            if (metapropertyMap.Count == 0) return;

            // check if any of the updated fields is in the mapping
            if (fields.Any(field => metapropertyMap.Any(map=> Equals(field, map.InriverFieldTypeId))))
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

        #endregion Methods
    }
}