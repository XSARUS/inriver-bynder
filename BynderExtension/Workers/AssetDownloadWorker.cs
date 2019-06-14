using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Names;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;

namespace Bynder.Workers
{
    class AssetDownloadWorker : IWorker
    {
        private readonly inRiverContext _inRiverContext;
        private readonly IBynderClient _bynderClient;

        public AssetDownloadWorker(inRiverContext inRiverContext, IBynderClient bynderClient = null)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        public void Execute(Entity resourceEntity)
        {
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;
            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);
            }

            string bynderDownloadState =
                (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState)?.Data;

            if (string.IsNullOrWhiteSpace(bynderDownloadState) || bynderDownloadState != BynderStates.Todo) return;

            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // download asset information
            Asset asset = _bynderClient.GetAssetByAssetId(bynderId);
            if (asset == null)
            {
                _inRiverContext.Log(LogLevel.Error, "Asset information is empty");
                return;
            }

            // check for existing file
            var resourceFileId = resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data;
            int existingFileId = resourceFileId != null ? (int)resourceFileId : 0;

            // add new asset
            string resourceFileName = (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(resourceFileName, _bynderClient.GetAssetDownloadLocation(asset.Id).S3_File);

            // delete older asset file
            if (existingFileId > 0)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                _inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId);
            }

            // set fieltypes for resource entity
            resourceEntity.GetField(FieldTypeIds.ResourceFileId).Data = newFileId;
            resourceEntity.GetField(FieldTypeIds.ResourceMimeType).Data = asset.GetOriginalMimeType();
            resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState).Data = BynderStates.Done;

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            _inRiverContext.Logger.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
        }
    }
}