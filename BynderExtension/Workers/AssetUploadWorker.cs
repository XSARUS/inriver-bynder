using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Bynder.Workers
{
    using Api;
    using Api.Model;
    using Exceptions;
    using Names;
    using Utils.Helpers;

    public class AssetUploadWorker : IWorker
    {
        #region Fields

        private const int CHUNK_SIZE = 1024 * 1024 * 5;
        private const int MAX_POLLING_ITERATIONS = 60;
        private const int POLLING_IDDLE_TIME = 2000;

        private readonly IBynderClient _bynderClient;
        private readonly inRiverContext _inRiverContext;

        #endregion Fields

        #region Constructors

        public AssetUploadWorker(inRiverContext inRiverContext, IBynderClient bynderClient = null)
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

            var brands = _bynderClient.GetAvailableBranches();
            return brands
                .FirstOrDefault(b => b.name.Equals(brandName, StringComparison.InvariantCultureIgnoreCase))
                ?.Id;
        }

        private string GetBynderUploadStateFromEntity(Entity resourceEntity)
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

            var s3Bucket = _bynderClient.GetClosestS3Endpoint();
            if (s3Bucket == null)
            {
                throw new MissingDataException($"Upload resource entity {resourceEntity.Id} failed, because the amazon s3 bucket endpoint cannot be defined!");
            }

            var uploadRequest = _bynderClient.RequestUploadInformation(new RequestUploadQuery { Filename = filename });
            if (uploadRequest == null)
            {
                throw new MissingDataException($"Upload resource entity {resourceEntity.Id} failed, because we could not get upload information from Bynder!");
            }

            return new ResourceUploadData
            {
                BrandId = brandId,
                Filename = filename,
                FileId = fileId,
                Bytes = bytes,
                S3Bucket = s3Bucket,
                UploadRequest = uploadRequest
            };
        }

        private int GetFileIdFromEntity(Entity resourceEntity)
        {
            var resourceFileId = resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data;
            return resourceFileId != null ? (int)resourceFileId : 0;
        }

        private string GetFileNameFromEntity(Entity resourceEntity)
        {
            return (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
        }

        private byte[] GetResourceByteArrayFromEntity(int fileId)
        {
            return _inRiverContext.ExtensionManager.UtilityService.GetFile(fileId, "Original");
        }

        private bool HasFinishedSuccessfully(FinalizeResponse finalizeResponse)
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
        }

        private uint UploadResourceAsChunksToS3Bucket(string fileName, byte[] bytes, string s3Bucket, UploadRequest uploadRequest)
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
        }

        private void UploadResourceForEntity(Entity resourceEntity)
        {
            try
            {
                var resourceUploadData = GetDataForUpload(resourceEntity);
                uint chunkNumber = UploadResourceAsChunksToS3Bucket(resourceUploadData.Filename, resourceUploadData.Bytes, resourceUploadData.S3Bucket, resourceUploadData.UploadRequest);
                var finalizeResponse = _bynderClient.FinalizeUpload(resourceUploadData.UploadRequest, chunkNumber);
                if (HasFinishedSuccessfully(finalizeResponse))
                {
                    var result = _bynderClient.SaveMedia(new SaveMediaQuery()
                    {
                        MediaId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId)?.Data,
                        BrandId = resourceUploadData.BrandId,
                        Filename = resourceUploadData.Filename,
                        ImportId = finalizeResponse.ImportId
                    });

                    resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId).Data = result.MediaId;
                }

                resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Done;
                _inRiverContext.Log(LogLevel.Information, $"Finished uploading resource entity {resourceEntity.Id}");
            }
            catch (Exception ex)
            {
                resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                _inRiverContext.Log(LogLevel.Error, $"Error uploading resource entity {resourceEntity.Id}. Message: {ex.GetBaseException().Message}");
            }

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
        }

        #endregion Methods

        #region Classes

        private class ResourceUploadData
        {
            #region Properties

            public string BrandId { get; set; }
            public byte[] Bytes { get; set; }
            public int FileId { get; set; }
            public string Filename { get; set; }
            public string S3Bucket { get; set; }
            public UploadRequest UploadRequest { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}