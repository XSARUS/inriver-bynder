using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Workers
{
    using Utils.Helpers;
    using Names;
    using Sdk.Model;
    using SettingProviders;
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    public class AssetDownloadWorker : AbstractBynderWorker, IWorker
    {
        #region Properties

        public override Dictionary<string, string> DefaultSettings => AssetDownloadWorkerSettingsProvider.Create();

        #endregion Properties

        #region Constructors

        public AssetDownloadWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null) :
            base(inRiverContext, bynderClient)
        {
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;

            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
            {
                resourceEntity = InRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);
            }

            // get the state field
            Field bynderDownloadStateField = resourceEntity.GetField(FieldTypeIds.ResourceBynderDownloadState);
            var bynderDownloadState = (string)bynderDownloadStateField?.Data;

            // stop when state is not equal to `todo`
            if (string.IsNullOrWhiteSpace(bynderDownloadState) || bynderDownloadState != BynderStates.Todo) return;

            string bynderId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderId)?.Data;
            if (string.IsNullOrWhiteSpace(bynderId)) return;

            // download asset information
            Media media = _bynderClient.GetAssetService().GetAssetByMediaQuery(bynderId).GetAwaiter().GetResult();

            if (media == null)
            {
                InRiverContext.Log(LogLevel.Error, "Asset information is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            // check for existing file
            int existingFileId = (int?)resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data ?? 0;

            // add new asset
            Field resourceFilenameField = resourceEntity.GetField(FieldTypeIds.ResourceFilename);
            string resourceFilename = (string)resourceFilenameField?.Data;
            if (string.IsNullOrWhiteSpace(resourceFilename))
            {
                InRiverContext.Log(LogLevel.Error, $"Field '{FieldTypeIds.ResourceFilename}' is empty or does not exist");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            var (url, filename) = MediaHelper.GetDownloadUrlAndFilename(InRiverContext, _bynderClient, media).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(url))
            {
                InRiverContext.Log(LogLevel.Error, "File url is empty");

                // set error state when the asset could not be found
                bynderDownloadStateField.Data = BynderStates.Error;
                InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(new List<Field> { bynderDownloadStateField });
                return;
            }

            int newFileId = InRiverContext.ExtensionManager.UtilityService.AddFileFromUrl(filename, url);

            // delete older asset file
            if (existingFileId > 0)
            {
                InRiverContext.Log(LogLevel.Verbose, $"existing fileId found {existingFileId}");
                if (!InRiverContext.ExtensionManager.UtilityService.DeleteFile(existingFileId))
                {
                    InRiverContext.Log(LogLevel.Warning, $"Could not delete existing file with fileId {existingFileId} for resource entity {resourceEntity.Id}");
                }
            }

            // set fieldtypes for resource entity
            var resourceFileIdField = resourceEntity.GetField(FieldTypeIds.ResourceFileId);
            resourceFileIdField.Data = newFileId;

            var resourceMimeTypeField = resourceEntity.GetField(FieldTypeIds.ResourceMimeType);
            resourceMimeTypeField.Data = media.GetOriginalMimeType();

            bynderDownloadStateField.Data = BynderStates.Done;
            // We do NOT overwrite the filename because that leads to sync issues; it should be the same unless you change the download media-type
            // resourceFilenameField.Data = Filename;

            var fieldList = new List<Field> {
                    bynderDownloadStateField,
                    resourceFileIdField,
                    resourceMimeTypeField
                };

            // thumbnails | ticket #208787
            var thumbnailMappings = SettingHelper.GetFieldTypeThumbnailMappings(InRiverContext.Settings, InRiverContext.Logger);
            foreach (var thumbnailMapping in thumbnailMappings)
            {
                var thumbnailField = resourceEntity.GetField(thumbnailMapping.FieldTypeId);
                if (thumbnailField == null)
                {
                    InRiverContext.Log(LogLevel.Warning, $"Thumbnail field with fieldtype {thumbnailMapping.FieldTypeId} not found on resource entity {resourceEntity.Id}!");
                    continue;
                }

                var thumbnailUrl = GetThumbnailUrl(media, thumbnailMapping);

                if (thumbnailUrl == null)
                {
                    InRiverContext.Log(LogLevel.Warning, $"Thumbnail url for type '{thumbnailMapping.ThumbnailType}' or fallback-type '{thumbnailMapping.FallBackThumbnailType}' is not available on resource entity {resourceEntity.Id}!");
                    continue;
                }

                var clonedThumbnailField = thumbnailField.Clone() as Field;
                thumbnailField.Data = thumbnailUrl;
                if (thumbnailField.ValueHasBeenModified(clonedThumbnailField.Data))
                {
                    fieldList.Add(thumbnailField);
                }
            }

            try
            {
                resourceEntity = InRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(fieldList);
                InRiverContext.Log(LogLevel.Information, $"Updated resource entity {resourceEntity.Id}");
            }
            catch (Exception ex)
            {
                InRiverContext.Log(LogLevel.Error, "Could not update fields (" + string.Join(",", fieldList.Select(f => f.FieldType.Id)) + $") for  resource entity {resourceEntity.Id}: {ex.Message}", ex);
            }
        }

        protected string GetThumbnailUrl(Media media, Models.FieldTypeThumbnailMapping thumbnailMapping) =>
            MediaHelper.GetThumbnailUrl(InRiverContext, media, thumbnailMapping);

        #endregion Methods
    }
}