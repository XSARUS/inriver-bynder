using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;

namespace Bynder.Workers
{
    using Config;
    using Names;

    public class AssetDeletedWorker : IWorker
    {
        #region Fields

        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetDeletedWorker(inRiverContext inRiverContext)
        {
            _inRiverContext = inRiverContext;
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

            // only process if we are allowed to delete entities
            if (!GetDeleteResourceOnDeletedEvents()) return result;

            // find resourceEntity based on bynderAssetId
            Entity resourceEntity =
                _inRiverContext.ExtensionManager.DataService.GetEntityByUniqueValue(FieldTypeIds.ResourceBynderId, bynderAssetId,
                    LoadLevel.DataAndLinks);

            // delete if exist
            if (resourceEntity != null)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"Deleting Resource {resourceEntity.Id}, which is associated with the deleted bynder asset {bynderAssetId}");

                _inRiverContext.ExtensionManager.DataService.DeleteEntity(resourceEntity.Id);
                result.Messages.Add($"Deleted Resource {resourceEntity.Id}, which was associated with the deleted bynder asset {bynderAssetId}");
            }
            else
            {
                _inRiverContext.Log(LogLevel.Debug, $"Nothing to remove. Deleted asset {bynderAssetId} does not exist in inRiver as Resource.");
                result.Messages.Add($"Nothing to remove. Deleted asset {bynderAssetId} does not exist in inRiver as Resource.");
            }

            return result;
        }

        /// <summary>
        /// Optional setting. Default is false.
        /// </summary>
        /// <returns></returns>
        private bool GetDeleteResourceOnDeletedEvents()
        {
            if (_inRiverContext.Settings.ContainsKey(Settings.DeleteResourceOnDeleteEvent))
            {
                return string.Equals(_inRiverContext.Settings[Settings.DeleteResourceOnDeleteEvent], true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            _inRiverContext.Logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.DeleteResourceOnDeleteEvent}'. Using default value '{false}'");

            return false;
        }

        #endregion Methods
    }
}