using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Bynder.Workers
{
    using Api;
    using Bynder.Sdk.Model;
    using Exceptions;
    using Names;
    using Utils.Helpers;
    using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;
    using SdkUploadQuery = Sdk.Query.Upload.UploadQuery;

    public class AssetUploadWorker : IWorker
    {
        #region Fields

        private readonly SdkIBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetUploadWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null)
        {
            _inRiverContext = inRiverContext;
            _bynderClient = bynderClient;
        }

        #endregion Constructors

        #region Methods

        public void Execute(Entity resourceEntity)
        {
            _inRiverContext.Log(LogLevel.Information, $"Start uploading resource entity {resourceEntity.Id}");

            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;

            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
                resourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);

            string bynderUploadState = GetBynderUploadStateFromEntity(resourceEntity);
            if (string.IsNullOrWhiteSpace(bynderUploadState) || bynderUploadState != BynderStates.Todo) return;

            UploadResourceForEntity(resourceEntity);
        }

        private string GetBrandIdBasedOnSettingKey()
        {
            var brandName = SettingHelper.GetBynderBrandName(_inRiverContext.Settings, _inRiverContext.Logger);
            if (string.IsNullOrEmpty(brandName)) return null;

            var brands = _bynderClient.GetAssetService().GetBrandsAsync().GetAwaiter().GetResult();
            
            return brands
                .FirstOrDefault(b => b.Name.Equals(brandName, StringComparison.InvariantCultureIgnoreCase))
                ?.Id;
        }

        private static string GetBynderUploadStateFromEntity(Entity resourceEntity)
        {
            return (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
        }

        private ResourceUploadData GetDataForUpload(Entity resourceEntity)
        {
            string brandId = GetBrandIdBasedOnSettingKey();
            if (brandId == null)
            {
                throw new MissingDataException($"Upload resource entity {resourceEntity.Id} failed, because the brandname within settings is not correctly configurated!");
            }

            string filename = GetFileNameFromEntity(resourceEntity);
            if (filename == null)
            {
                throw new MissingDataException($"Upload resource entity {resourceEntity.Id} failed, because the filename within the entity is not set!");
            }

            int fileId = GetFileIdFromEntity(resourceEntity);

            byte[] bytes = GetResourceByteArrayFromEntity(fileId);
            if (bytes.Length == 0)
            {
                throw new MissingDataException($"Upload resource entity {resourceEntity.Id} failed, because there is no resource avaiable!");
            }

            return new ResourceUploadData
            {
                BrandId = brandId,
                Filename = filename,
                FileId = fileId,
                Bytes = bytes
            };
        }

        private static int GetFileIdFromEntity(Entity resourceEntity)
        {
            var resourceFileId = resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data;
            return resourceFileId != null ? (int)resourceFileId : 0;
        }

        private static string GetFileNameFromEntity(Entity resourceEntity)
        {
            return (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
        }

        private byte[] GetResourceByteArrayFromEntity(int fileId)
        {
            return _inRiverContext.ExtensionManager.UtilityService.GetFile(fileId, "Original");
        }

        /*private bool HasFinishedSuccessfully(FinalizeResponse finalizeResponse)
        {
            for (int iterations = MAX_POLLING_ITERATIONS; iterations > 0; --iterations)
            {
                var pollStatus = _bynderClient.PollStatus(new string[] { finalizeResponse.ImportId });
                if (pollStatus != null)
                {
                    if (pollStatus.ItemsDone.Contains(finalizeResponse.ImportId))
                        return true;

                    if (pollStatus.ItemsFailed.Contains(finalizeResponse.ImportId))
                        return false;
                }

                Thread.Sleep(POLLING_IDDLE_TIME);
            }

            return false;
        }*/

        /*private uint UploadResourceAsChunksToS3Bucket(string fileName, byte[] bytes, string s3Bucket, UploadRequest uploadRequest)
        {
            uint chunkNumber = 0;

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                int bytesRead = 0;
                var buffer = new byte[CHUNK_SIZE];
                long numberOfChunks = (stream.Length + CHUNK_SIZE - 1) / CHUNK_SIZE;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ++chunkNumber;
                    _bynderClient.UploadPart(s3Bucket, fileName, buffer, bytesRead, chunkNumber, uploadRequest, (uint)numberOfChunks);
                }
            }

            return chunkNumber;
        }*/

        private void UploadResourceForEntity(Entity resourceEntity)
        {
            var fieldsToUpdate = new List<Field>();
            Field bynderUploadStateField = resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState);
            try
            {
                var resourceUploadData = GetDataForUpload(resourceEntity);

                var fileStream = new MemoryStream(resourceUploadData.Bytes, writable: false);
                SaveMediaResponse uploadResult = _bynderClient.GetAssetService().UploadFileAsync(fileStream, new SdkUploadQuery()
                {
                    BrandId = resourceUploadData.BrandId,
                    Name = resourceUploadData.Filename,
                    MediaId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId)?.Data,
                }).GetAwaiter().GetResult();
                
                if (uploadResult.IsSuccessful)
                {
                    var bynderAssetIdField = resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId);
                    bynderAssetIdField.Data = uploadResult.MediaId;
                    fieldsToUpdate.Add(bynderAssetIdField);
                }

                bynderUploadStateField.Data = BynderStates.Done;
                _inRiverContext.Log(LogLevel.Information, $"Finished uploading resource entity {resourceEntity.Id}");
            }
            catch (Exception ex)
            {
                bynderUploadStateField.Data = BynderStates.Error;
                _inRiverContext.Log(LogLevel.Error, $"Error uploading resource entity {resourceEntity.Id}. Message: {ex.GetBaseException().Message}");
            }

            fieldsToUpdate.Add(bynderUploadStateField);

            _inRiverContext.ExtensionManager.DataService.UpdateFieldsForEntity(fieldsToUpdate);
        }

        #endregion Methods

        #region Classes

        private sealed class ResourceUploadData
        {
            #region Properties

            public string BrandId { get; set; }
            public byte[] Bytes { get; set; }
            public int FileId { get; set; }
            public string Filename { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}