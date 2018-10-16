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
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeId.Resource)) return;
            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);
            }

            string bynderDownloadState =
                resourceEntity.GetField(FieldTypeId.ResourceBynderDownloadState)?.Data
                    ?.ToString();

            if (string.IsNullOrWhiteSpace(bynderDownloadState) || bynderDownloadState != BynderState.Todo) return;

            string bynderId = resourceEntity.GetField(FieldTypeId.ResourceBynderId)?.Data?.ToString();
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // download asset information
            Asset asset = _bynderClient.GetAssetByAssetId(bynderId);
            if (asset == null)
            {
                _inRiverContext.Log(LogLevel.Error, "Asset information is empty");
                return;
            }

            // check for existing file
            int existingFileId = 0;
            if (resourceEntity.GetField(FieldTypeId.ResourceFileId).Data != null)
            {
                existingFileId = (int)resourceEntity.GetField(FieldTypeId.ResourceFileId).Data;
            }

            // add new asset
            string resourceFileName = resourceEntity.GetField(FieldTypeId.ResourceFileName).Data.ToString();
            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(resourceFileName, _bynderClient.GetAssetDownloadLocation(asset.Id).S3_File);

            // delete older asset file
            if (existingFileId > 0)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                _inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId);
            }

            // set fieltypes for resource entity
            resourceEntity.GetField(FieldTypeId.ResourceFileId).Data = newFileId;
            resourceEntity.GetField(FieldTypeId.ResourceMimeType).Data = asset.GetOriginalMimeType();
            resourceEntity.GetField(FieldTypeId.ResourceBynderDownloadState).Data = BynderState.Done;

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            _inRiverContext.Logger.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
        }
    }
}