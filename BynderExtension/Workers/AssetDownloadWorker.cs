using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bynder.Workers
{
    using Api;
    using Model;
    using Bynder.Models;
    using Bynder.Sdk.Model;
    using Bynder.Sdk.Query.Asset;
    using Names;
    using Utils.Helpers;
    using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;

    public class AssetDownloadWorker : IWorker
    {
        #region Fields

        private readonly SdkIBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetDownloadWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null)
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
            Media media = _bynderClient.GetAssetService().GetAssetByMediaQuery(bynderId).GetAwaiter().GetResult();

            if (media == null)
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
            Tuple<string, string> fileHandlingDetails = GetDownloadUrlAndFilename(media).GetAwaiter().GetResult();
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
                if (!_inRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId))
                {
                    _inRiverContext.Log(LogLevel.Warning, $"Could not delete existing file with fileId {existingFileId} for resource entity {resourceEntity.Id}");
                }
            }

            // set fieldtypes for resource entity
            var resourceFileIdField = resourceEntity.GetField(FieldTypeIds.ResourceFileId);
            resourceFileIdField.Data = newFileId;

            var resourceMimeTypeField = resourceEntity.GetField(FieldTypeIds.ResourceMimeType);
            resourceMimeTypeField.Data = media.GetOriginalMimeType();

            bynderDownloadStateField.Data = BynderStates.Done;
            resourceFilenameField.Data = fileHandlingDetails.Item2;

            var fieldList = new List<Field> {
                    resourceFilenameField,
                    bynderDownloadStateField,
                    resourceFileIdField,
                    resourceMimeTypeField
                };

            try
            {
                resourceEntity = _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
                _inRiverContext.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
            }
            catch (Exception ex)
            {
                _inRiverContext.Log(LogLevel.Error, "Could not update fields (" + string.Join(",", fieldList.Select(f => f.FieldType.Id))  + $") for  resource entity {resourceEntity.Id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Returns the download URL and filename to use
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public async Task<Tuple<string, string>> GetDownloadUrlAndFilename(Media asset)
        {
            var originalFileExtension = Path.GetExtension(asset.GetOriginalFileName()).Replace(".", "").ToLower();
            var mappings = SettingHelper.GetFilenameExtensionMediaTypeMapping(_inRiverContext.Settings, _inRiverContext.Logger);

            // Loop through all mappings if the file-extension has any mappings configured.
            // Use the first mapping which applies and skip the rest 
            if (mappings.ContainsKey(originalFileExtension)) {
                foreach (var mapping in mappings[originalFileExtension])
                {
                    if (asset.Thumbnails.All.ContainsKey(mapping.MediaType))
                    {
                        string downloadUrl = asset.Thumbnails.All[mapping.MediaType].Value<string>();
                        var uri = new Uri(downloadUrl);
                        string formattedFilename = Path.GetFileName(uri.LocalPath);
                        string extension = Path.GetExtension(formattedFilename);

                        if (string.IsNullOrWhiteSpace(extension)) {
                            formattedFilename = asset.GetOriginalFileName();
                        }                      

                        if (!string.IsNullOrWhiteSpace(mapping.FilenameRegex?.Trim()))
                        {
                            string regexPattern = mapping.FilenameRegex?.Replace(@"\\", @"\");
                            formattedFilename = Regex.Replace(formattedFilename, regexPattern, "");
                        }

                        return new Tuple<string, string>(downloadUrl, formattedFilename);
                    }
                }
            }

            // Default to original when no mapping is found
            string downloadMediaType = SettingHelper.GetDownloadMediaType(_inRiverContext.Settings, _inRiverContext.Logger);

            if (downloadMediaType.Equals("original"))
            {
                Uri assetDownloadLocation = await _bynderClient.GetAssetService().GetDownloadFileUrlAsync(new DownloadMediaQuery() { MediaId = asset.Id });
                return new Tuple<string, string>(assetDownloadLocation.ToString(), asset.GetOriginalFileName());
            }

            if (asset.Thumbnails.All.ContainsKey(downloadMediaType))
            {
                return new Tuple<string, string>(
                    asset.Thumbnails.All[downloadMediaType].Value<string>(), 
                    asset.MediaItems.FirstOrDefault(mi => mi.Type.Equals(downloadMediaType, StringComparison.OrdinalIgnoreCase))?.Name ?? asset.GetOriginalFileName()
                );
            }

            _inRiverContext.Log(LogLevel.Warning, $"Download media type (original or a derivative/thumbnail) '{downloadMediaType}' not found!");
            return null;
        }

        #endregion Methods
    }
}