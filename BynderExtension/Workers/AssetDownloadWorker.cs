using System.Collections.Generic;
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

            var bynderDownloadStateField = resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState);
            string bynderDownloadState = (string)bynderDownloadStateField?.Data;

            if (string.IsNullOrWhiteSpace(bynderDownloadState) || bynderDownloadState != BynderStates.Todo) return;

            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // download asset information
            Asset asset = _bynderClient.GetAssetByAssetId(bynderId);
            if (asset == null)
            {
                _inRiverContext.Log(LogLevel.Error, "Asset information is empty");

                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });

                return;
            }

            // check for existing file
            var resourceFileId = resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data;
            var existingFileId = (int?)resourceFileId ?? 0;

            // download the asset
            var file = _bynderClient.DownloadAsset(asset.Id);
            if (file == null)
            {
                _inRiverContext.Log(LogLevel.Error, $"Could not download asset with Id {asset.Id}");

                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });

                return;
            }

            if (existingFileId > 0)
            {
                // check if existing ResourceFile is same size as the one in Bynder
                var resourceFileMetaData = _inRiverContext.ExtensionManager.UtilityService.GetFileMetaData(existingFileId);
                if (file.LongLength == resourceFileMetaData.FileSize)
                {
                    _inRiverContext.Log(LogLevel.Debug, "Asset is the same as before. Setting the state to Done.");
                    bynderDownloadStateField.Data = BynderStates.Done;
                    _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                    return;
                }
                // delete older asset file
                _inRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                _inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId);
            }

            // add new asset
            string resourceFileName = (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFile(resourceFileName, file);

            // set fieltypes for resource entity
            resourceEntity.GetField(FieldTypeIds.ResourceFileId).Data = newFileId;
            resourceEntity.GetField(FieldTypeIds.ResourceMimeType).Data = asset.GetOriginalMimeType();
            bynderDownloadStateField.Data = BynderStates.Done;

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            _inRiverContext.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
        }
    }
}