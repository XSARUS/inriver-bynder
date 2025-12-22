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
    using System.Text.RegularExpressions;
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
            Field resourceFilenameField = resourceEntity.GetField(FieldTypeIds.ResourceFilename);
            string resourceFilename = (string)resourceFilenameField?.Data;
            if (string.IsNullOrWhiteSpace(resourceFilename))
            {
                _inRiverContext.Log(LogLevel.Error, $"Field '{FieldTypeIds.ResourceFilename}' is empty or does not exist");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            /** 1 = download url, 2 = formatted filename */
            Tuple<string, string> fileHandlingDetails = GetDownloadUrlAndFilename(asset);
            if (string.IsNullOrWhiteSpace(fileHandlingDetails.Item1))
            {
                _inRiverContext.Log(LogLevel.Error, "File url is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            int newFileId = _inRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(fileHandlingDetails.Item2, fileHandlingDetails.Item1);

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
            resourceFilenameField.Data = fileHandlingDetails.Item2;

            resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> {
                resourceFilenameField,
                bynderDownloadStateField, 
                resourceFileIdField, 
                resourceMimeTypeField
            });

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

            foreach (var mediaTypeConfig in mapping.MediaTypeConfiguration)
            {
                MediaItem mediaItem = asset.MediaItems.FirstOrDefault(mi => mi.Type.Equals(mediaTypeConfig.MediaType, StringComparison.OrdinalIgnoreCase));
                if (mediaItem == null)
                {
                    continue;
                }

                if (asset.Thumbnails.ContainsKey(mediaTypeConfig.MediaType))
                {
                    string formattedFilename = asset.MediaItems.FirstOrDefault(mi => mi.Type.Equals(mediaTypeConfig.MediaType, StringComparison.OrdinalIgnoreCase))?.FileName ?? asset.GetOriginalFileName();
                    
                    if (!string.IsNullOrWhiteSpace(mediaTypeConfig.FilenameRegex?.Trim()))
                    {
                        formattedFilename = Regex.Replace(formattedFilename, @mediaTypeConfig.FilenameRegex, "");
                    }
                    
                    return new Tuple<string, string>(asset.Thumbnails[mediaTypeConfig.MediaType], formattedFilename);
                }
            }

            // Default to original when no mapping is found
            string downloadMediaType = SettingHelper.GetDownloadMediaType(_inRiverContext.Settings, _inRiverContext.Logger);

            if (downloadMediaType.Equals("original"))
            {
                return new Tuple<string, string>(_bynderClient.GetAssetDownloadLocation(asset.Id)?.S3_File, asset.GetOriginalFileName());
            }

            if (asset.Thumbnails.ContainsKey(downloadMediaType))
            {
                return new Tuple<string, string>(
                    asset.Thumbnails[downloadMediaType], 
                    asset.MediaItems.FirstOrDefault(mi => mi.Type.Equals(downloadMediaType, StringComparison.OrdinalIgnoreCase))?.FileName ?? asset.GetOriginalFileName()
                );
            }

            _inRiverContext.Log(LogLevel.Warning, $"Download media type (original or a derivative/thumbnail) '{downloadMediaType}' not found!");
            return null;
        }

        #endregion Methods
    }
}