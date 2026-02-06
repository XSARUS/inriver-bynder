using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Workers
{
    using SettingProviders;
    using Models;
    using Names;
    using Utils.Helpers;

    public class AssetDeletedWorker : AbstractWorker, IWorker
    {
        public override Dictionary<string, string> DefaultSettings => AssetDeletedWorkerSettingsProvider.Create();

        #region Constructors

        public AssetDeletedWorker(inRiverContext inRiverContext) : base(inRiverContext)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Deletes the resource in inRiver, associated with the asset id of bynder
        /// </summary>
        /// <param name="bynderAssetId"></param>
        /// <returns></returns>
        public WorkerResult Execute(string bynderAssetId)
        {
            var result = new WorkerResult();
            var process = SettingHelper.GetDeleteResourceOnDeletedEvents(InRiverContext.Settings, InRiverContext.Logger);

            // only process if we are allowed to delete entities
            if (!process)
            {
                InRiverContext.Log(LogLevel.Debug, $"Value for 'AssetDeletedWorker' process-var: {process.ToString()}");
                return result;
            }

            // find resourceEntity based on bynderAssetId. We can't retreive the filename anymore so we have to match on bynder id. That is no problem, because it is always set also on update.
            Entity resourceEntity =
                InRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceBynderId, bynderAssetId,
                    LoadLevel.DataAndLinks);

            // delete if exist
            if (resourceEntity != null)
            {
                InRiverContext.Log(LogLevel.Verbose, $"Deleting Resource {resourceEntity.Id}, which is associated with the deleted bynder asset {bynderAssetId}");

                InRiverContext.ExtensionManager.DataService.DeleteEntity(resourceEntity.Id);
                result.Messages.Add($"Deleted Resource {resourceEntity.Id}, which was associated with the deleted bynder asset {bynderAssetId}");
            }
            else
            {
                InRiverContext.Log(LogLevel.Debug, $"Nothing to remove. Deleted asset {bynderAssetId} does not exist in inRiver as Resource.");
                result.Messages.Add($"Nothing to remove. Deleted asset {bynderAssetId} does not exist in inRiver as Resource.");
            }

            return result;
        }

        #endregion Methods
    }
}