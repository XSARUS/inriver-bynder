using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using Names;
    using SettingProviders;
    using Utils.Helpers;

    public class NonResourceMetapropertyWorker : AbstractWorker, IWorker
    {
        #region Fields

        private readonly ResourceMetapropertyUpdateWorker _resourceMetapropertyUpdateWorker;

        #endregion Fields

        #region Properties

        public override Dictionary<string, string> DefaultSettings => NonResourceMetapropertyWorkerSettingsProvider.Create();

        #endregion Properties

        #region Constructors

        public NonResourceMetapropertyWorker(inRiverContext inRiverContext, ResourceMetapropertyUpdateWorker resourceMetapropertyUpdateWorker) : base(inRiverContext)
        {
            _resourceMetapropertyUpdateWorker = resourceMetapropertyUpdateWorker;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity entity, string[] fields)
        {
            if (entity.EntityType.Id == EntityTypeIds.Resource) return;

            // create metaproperty dictionary
            var metapropertyMap = SettingHelper.GetConfiguredMetaPropertyMap(InRiverContext.Settings, InRiverContext.Logger);
            if (metapropertyMap.Count == 0) return;

            // check if any of the updated fields is in the mapping
            if (fields.Any(field => metapropertyMap.Any(map => Equals(field, map.InriverFieldTypeId))))
            {
                var resourceIds = InRiverContext.ExtensionManager.DataService.GetOutboundLinksForEntity(entity.Id)
                    .Where(l => l.LinkType.TargetEntityTypeId.Equals(EntityTypeIds.Resource))
                    .Select(l => l.Target.Id);

                foreach (var resourceId in resourceIds)
                {
                    var targetResourceEntity =
                        InRiverContext.ExtensionManager.DataService.GetEntity(resourceId, LoadLevel.DataOnly);

                    // use the other worker for processing this resource
                    _resourceMetapropertyUpdateWorker.Execute(targetResourceEntity);
                }
            }
        }

        #endregion Methods
    }
}