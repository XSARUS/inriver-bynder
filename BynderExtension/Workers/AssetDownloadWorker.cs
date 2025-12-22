using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;

namespace Bynder.Workers
{
    using Api;
    using Api.Model;
    using Bynder.Models;
    using Names;
    using System;
    using System.IO;
    using System.Linq;
    using Utils.Helpers;

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

            Tuple<string, string> fileUrl = GetDownloadUrlAndFilename(asset);
            if (string.IsNullOrWhiteSpace(fileUrl.Item1))
            {
                _inRiverContext.Log(LogLevel.Error, "File url is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(resourceFilename, fileUrl.Item1);

            // delete older asset file
            if (existingFileId > 0)
            {
                _inRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                _inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId);
            }

            // set fieldtypes for resource entity
            var resourceFileIdField = resourceEntity.GetField(FieldTypeIds.ResourceFileId);
            resourceFileIdField.Data = newFileId;

            var resourceMimeTypeField = resourceEntity.GetField(FieldTypeIds.ResourceMimeType);
            resourceMimeTypeField.Data = asset.GetOriginalMimeType();

            bynderDownloadStateField.Data = BynderStates.Done;

            resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField, resourceFileIdField, resourceMimeTypeField });
            _inRiverContext.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
        }

        /// <summary>
        /// Returns the download URL and filename to use
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private Tuple<string, string> GetDownloadUrlAndFilename(Asset asset)
        {
            var originalFileExtension = Path.GetExtension(asset.GetOriginalFileName()).Replace(".", "").ToLower();
            FilenameExtensionMediaTypeMapping mapping = SettingHelper
                .GetFilenameExtensionMediaTypeMapping(_inRiverContext.Settings, _inRiverContext.Logger)
                .FirstOrDefault(m => m.FileExtension.Equals(originalFileExtension, StringComparison.OrdinalIgnoreCase));

            // Default to original when no mapping is found
            if (mapping == null)
            {
                string downloadMediaType = SettingHelper.GetDownloadMediaType(_inRiverContext.Settings, _inRiverContext.Logger);

                if (downloadMediaType.Equals("original"))
                {
                    return new Tuple<string, string>(_bynderClient.GetAssetDownloadLocation(asset.Id)?.S3_File, asset.GetOriginalFileName());
                }

                if (asset.Thumbnails.ContainsKey(downloadMediaType))
                {
                    return new Tuple<string, string>(asset.Thumbnails[downloadMediaType], asset.Thumbnails[downloadMediaType]);
                }

                _inRiverContext.Log(LogLevel.Warning, $"Download media type (original or a derivative/thumbnail) '{downloadMediaType}' not found!");
                return null;
            }



            return null;
        }

        #endregion Methods
    }
}