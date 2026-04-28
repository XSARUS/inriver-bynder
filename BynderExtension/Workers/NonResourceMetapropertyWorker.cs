using inRiver.Remoting.Log;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly EntityTraverser _entityTraverser;

        #endregion Fields

        #region Properties

        public override Dictionary<string, string> DefaultSettings => NonResourceMetapropertyWorkerSettingsProvider.Create();

        #endregion Properties

        #region Constructors

        public NonResourceMetapropertyWorker(inRiverContext inRiverContext, EntityTraverser entityTraverser, ResourceMetapropertyUpdateWorker resourceMetapropertyUpdateWorker) : base(inRiverContext)
        {
            _entityTraverser = entityTraverser;
            _resourceMetapropertyUpdateWorker = resourceMetapropertyUpdateWorker;
        }

        #endregion Constructors

        #region Methods

        public async Task Execute(Entity entity, string[] fields)
        {
            if (entity.EntityType.Id == EntityTypeIds.Resource) return;

            // create metaproperty dictionary
            var configuredMetaPropertyMap = SettingHelper.GetConfiguredMetaPropertyMapToBynder(InRiverContext.Settings, InRiverContext.Logger);
            if (configuredMetaPropertyMap == null) return;

            // check if any of the updated fields is in the mapping
            if (!MetaPropertyTraverseConfigHelper.HasAnyConfiguredField(fields, configuredMetaPropertyMap)) return;

            // get start entities 
            var startEntityIds = _entityTraverser.GetStartEntityIds(entity,configuredMetaPropertyMap);

            // pass resource to the resource Metaproperty Update Worker so we can export the metaproperties
            var resources = InRiverContext.ExtensionManager.DataService.GetEntities(startEntityIds, LoadLevel.DataOnly);
            var tasks = new List<Task>(resources.Count);
            foreach (var resource in resources)
            {
                tasks.Add(_resourceMetapropertyUpdateWorker.Execute(resource));
            }

            var task = Task.WhenAll(tasks);

            try
            {
                await task;
            }
            catch (AggregateException ex)
            {
                InRiverContext.Log(LogLevel.Error, $"An exception occurred executing the workers for NonResourceMetapropertyWorker for {resources.Count} resources (Task status: {task.Status}): {ex.GetBaseException().Message}", ex);
            }
            catch (Exception ex)
            {
                InRiverContext.Log(LogLevel.Error, $"An exception occurred executing the workers NonResourceMetapropertyWorker for {resources.Count} resources (Task status: {task.Status}): {ex.GetBaseException().Message}", ex);
            }
        }

        #endregion Methods
    }
}