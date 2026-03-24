using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Workers
{
    using Names;
    using SettingProviders;
    using Utils.Helpers;
    using Utils.Traverser;

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
            var configuredMetaPropertyMap = SettingHelper.GetConfiguredMetaPropertyMapToBynder(InRiverContext.Settings, InRiverContext.Logger);
            if (configuredMetaPropertyMap == null) return;

            // check if any of the updated fields is in the mapping
            if (!MetaPropertyTraverseConfigHelper.HasAnyConfiguredField(fields, configuredMetaPropertyMap)) return;

            // get start entities 
            var entityTraverser = new EntityTraverser(InRiverContext);
            var startEntityIds = entityTraverser.GetStartEntityIds(entity,configuredMetaPropertyMap);

            // pass resource to the resource Metaproperty Update Worker so we can export the metaproperties
            var resources = InRiverContext.ExtensionManager.DataService.GetEntities(startEntityIds, LoadLevel.DataOnly);
            foreach (var resource in resources)
            {
                _resourceMetapropertyUpdateWorker.Execute(resource);
            }
        }

        #endregion Methods
    }
}