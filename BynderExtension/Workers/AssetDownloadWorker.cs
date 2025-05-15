using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Names;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Workers
{
    internal class AssetDownloadWorker : IWorker
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetDownloadWorker(inRiverContext inRiverContext, IBynderClient bynderClient = null)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;
            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);
            }

            // get the state field
            Field bynderDownloadStateField = resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState);
            var bynderDownloadState = (string)bynderDownloadStateField?.Data;

            // stop when state is not todo
            if (string.IsNullOrWhiteSpace(bynderDownloadState) || bynderDownloadState != BynderStates.Todo) return;

            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // download asset information
            Asset asset = _bynderClient.GetAssetByAssetId(bynderId);
            if (asset == null)
            {
                _inRiverContext.Log(LogLevel.Error, "Asset information is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            // check for existing file
            int existingFileId = (int?)resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data ?? 0;

            // add new asset
            string resourceFilename = (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
            if (string.IsNullOrWhiteSpace(resourceFilename))
            {
                _inRiverContext.Log(LogLevel.Error, $"Field '{FieldTypeIds.ResourceFilename}' is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            string fileUrl = GetFileUrl(asset);
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                _inRiverContext.Log(LogLevel.Error, "File url is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(resourceFilename, fileUrl);

            // delete older asset file
            if (existingFileId > 0)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                _inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId);
            }

            // set fieltypes for resource entity
            resourceEntity.GetField(FieldTypeIds.ResourceFileId).Data = newFileId;
            resourceEntity.GetField(FieldTypeIds.ResourceMimeType).Data = asset.GetOriginalMimeType();
            bynderDownloadStateField.Data = BynderStates.Done;

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            _inRiverContext.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
        }

        private string GetFileUrl(Asset asset)
        {
            string downloadMediaType;

            if (!_inRiverContext.Settings.TryGetValue(Config.Settings.DownloadMediaType, out downloadMediaType))
            {
                downloadMediaType = "Original";
            }

            // todo does the case matter?
            if (downloadMediaType.Equals("Original", System.StringComparison.InvariantCultureIgnoreCase)) 
            {
                return _bynderClient.GetAssetDownloadLocation(asset.Id).S3_File;
            }

            if (asset.Thumbnails.ContainsKey(downloadMediaType))
            {
                return asset.Thumbnails[downloadMediaType];
            }

            //todo do we throw an exception, do we return original and log a warning?
            throw new KeyNotFoundException($"Download media type (Original, derivative/thumbnail) '{downloadMediaType}' not found!");
        }

        #endregion Methods
    }
}