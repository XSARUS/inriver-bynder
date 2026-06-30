using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Config;
    using Models;
    using SettingProviders;
    using Utils.Helpers;
    using Workers;

    public class InriverEventHandler : AbstractScheduledExtension
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;

                foreach (var setting in AssetDownloadWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in ResourceMetapropertyUpdateWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in AssetUsageUpdateWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                foreach (var setting in NonResourceMetapropertyWorkerSettingsProvider.Create())
                {
                    settings[setting.Key] = setting.Value;
                }

                settings.Add(Settings.MaxRetryAttempts, Settings.DefaultMaxRetryAttempts.ToString());

                return settings;
            }
        }

        #endregion Properties

        #region Methods

        protected override void Execute()
        {
            try
            {
                List<ConnectorState> states = GetConnectorStates();
                if (states.Count == 0) return;

                Context.Log(LogLevel.Information, $"Start handling of {states.Count} Inriver events");

                var workers = GetWorkers();
                var processingStatistics = new ProcessingStatistics();
                int maxRetryAttempts = SettingHelper.GetMaxRetryAttempts(Context.Settings, Context.Logger);

                foreach (ConnectorState state in states.OrderBy(s => s.Created))
                {
                    ProcessState(workers, processingStatistics, maxRetryAttempts, state);
                }

                Context.Log(LogLevel.Information, $"Finished handling of {states.Count} Inriver events [{processingStatistics.Successful} success | {processingStatistics.Failed} failed | {processingStatistics.Retried} retried]");
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.Message, ex);
            }
        }

        private static void HandleInriverEvent(WorkerContainer workers, InriverEvent stateData, Entity entity)
        {
            if (stateData.IsResource && stateData.IsLink)
            {
                // only update the metaproperties on asset
                workers.ResourceMetapropertyUpdateWorker
                    .Execute(entity)
                    .GetAwaiter()
                    .GetResult();
            }
            else if (stateData.IsResource)
            {
                // (re-)download asset
                workers.AssetDownloadWorker
                        .Execute(entity)
                        .GetAwaiter()
                        .GetResult();

                // update asset metaproperties and update asset usage
                Task.WhenAll(
                    workers.ResourceMetapropertyUpdateWorker.Execute(entity),
                    workers.AssetUsageUpdateWorker.Execute(entity))
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                // handle updates of non-resources
                workers.NonResourceMetapropertyWorker
                    .Execute(entity, stateData.FieldTypeIds)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private List<ConnectorState> GetConnectorStates()
        {
            return Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Names.ConnectorStateIds.BynderInriverEvents);
        }

        private WorkerContainer GetWorkers()
        {
            return new WorkerContainer
            {
                ResourceMetapropertyUpdateWorker =
                        Container.GetInstance<ResourceMetapropertyUpdateWorker>(),

                NonResourceMetapropertyWorker =
                        Container.GetInstance<NonResourceMetapropertyWorker>(),

                AssetDownloadWorker =
                        Container.GetInstance<AssetDownloadWorker>(),

                AssetUsageUpdateWorker =
                        Container.GetInstance<AssetUsageUpdateWorker>()
            };
        }

        private void ProcessState(WorkerContainer workers, ProcessingStatistics processingStatistics, int maxRetryAttempts, ConnectorState state)
        {
            var stateData = JsonConvert.DeserializeObject<InriverEvent>(state.Data);

            try
            {
                Context.Log(LogLevel.Debug, $"Handling Inriver event of ConnectorState {state.Id} created at {state.Created} attempt {stateData.Attempt}/{maxRetryAttempts}");

                var entity = Context.ExtensionManager.DataService.GetEntity(stateData.EntityId, LoadLevel.DataOnly);
                HandleInriverEvent(workers, stateData, entity);

                Context.Log(LogLevel.Debug, $"Handled Inriver event of ConnectorState {state.Id} created at {state.Created}");

                Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                processingStatistics.Successful++;
            }
            catch (Exception e)
            {
                if (e is AggregateException ae)
                {
                    foreach (var innerException in ae.InnerExceptions)
                    {
                        Context.Log(LogLevel.Error, $"An exception occurred handling the Inriver event of ConnectorState {state.Id} created at {state.Created}: {innerException.Message}", innerException);
                    }
                }

                Context.Log(LogLevel.Error, $"Failed handling Inriver event of ConnectorState {state.Id} created at {state.Created} [attempt {stateData.Attempt}/{maxRetryAttempts}]: {e.Message}", e);
                Context.Log(LogLevel.Verbose, $"Failed for ConnectorState {state.Id} created at {state.Created} with data: {state.Data}");

                if (stateData.Attempt < maxRetryAttempts)
                {
                    stateData.Attempt++;
                    state.Data = JsonConvert.SerializeObject(stateData);
                    Context.ExtensionManager.UtilityService.UpdateConnectorState(state);
                    processingStatistics.Retried++;
                }
                else
                {
                    Context.ExtensionManager.UtilityService.DeleteConnectorState(state.Id);
                    Context.Log(LogLevel.Error, $"Max retry attempts reached for ConnectorState {state.Id}", e);
                    processingStatistics.Failed++;
                }
            }
        }

        #endregion Methods

        #region Classes

        private sealed class WorkerContainer
        {
            #region Properties

            public AssetDownloadWorker AssetDownloadWorker { get; set; }
            public AssetUsageUpdateWorker AssetUsageUpdateWorker { get; set; }
            public NonResourceMetapropertyWorker NonResourceMetapropertyWorker { get; set; }
            public ResourceMetapropertyUpdateWorker ResourceMetapropertyUpdateWorker { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}
