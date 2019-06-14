using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Names;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Bynder.Workers
{
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
        private int GetFileIdFromEntity(Entity resourceEntity)
        {
            var resourceFileId = resourceEntity.GetField(FieldTypeIds.ResourceFileId)?.Data;
            return resourceFileId != null ? (int)resourceFileId : 0;         
        }
        private string GetFileNameFromEntity(Entity resourceEntity)
        {
            return (string)resourceEntity.GetField(FieldTypeIds.ResourceFilename)?.Data;
        }
        private string GetBrandIdBasedOnSettingKey()
        {
            if (!_inRiverContext.Settings.TryGetValue(Config.Settings.bynderBrandName, out var brandName)) return null;

            var brands = _bynderClient.GetAvailableBranches();
            return brands
                .FirstOrDefault(b => b.name.Equals(brandName, StringComparison.InvariantCultureIgnoreCase))
                ?.Id;
        }
        private string GetBynderUploadStateFromEntity(Entity resourceEntity)
        {
            return (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState)?.Data;
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

        public void Execute(Entity resourceEntity)
        {
            _inRiverContext.Logger.Log(LogLevel.Information, $"Start uploading resource entity {resourceEntity.Id}");

            if (!resourceEntity.EntityType.Id.Equals(EntityTypeIds.Resource)) return;

            if (resourceEntity.LoadLevel < LoadLevel.DataOnly)
                resourceEntity = _inRiverContext.ExtensionManager.DataService.GetEntity(resourceEntity.Id, LoadLevel.DataOnly);

            string bynderUploadState = GetBynderUploadStateFromEntity(resourceEntity);
            if (string.IsNullOrWhiteSpace(bynderUploadState) || bynderUploadState != BynderStates.Todo) return;

            try
            {
                string brandId = GetBrandIdBasedOnSettingKey();
                if (brandId == null)
                {
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"Upload resource entity {resourceEntity.Id} failed, because the brandname within settings is not correctly configurated");
                    resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                    return;
                }

                string fileName = GetFileNameFromEntity(resourceEntity);
                if (fileName == null)
                {
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"Upload resource entity {resourceEntity.Id} failed, because the filename within the entity is not set");
                    resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                    return;
                }

                int fileId = GetFileIdFromEntity(resourceEntity);

                byte[] bytes = GetResourceByteArrayFromEntity(fileId);
                if (bytes.Length == 0)
                {
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"Upload resource entity {resourceEntity.Id} failed, because there is no resource avaiable");
                    resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                    return;
                }

                var s3Bucket = _bynderClient.GetClosestS3Endpoint();
                if (s3Bucket == null)
                {
                    _inRiverContext.Logger.Log(LogLevel.Warning, $"Upload resource entity {resourceEntity.Id} failed, because the amazon s3 bucket endpoint cannot be defined");
                    resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                    return;
                }

                var uploadRequest = _bynderClient.RequestUploadInformation(new RequestUploadQuery { Filename = fileName });
                if (uploadRequest == null) return;

                uint chunkNumber = UploadResourceAsChunksToS3Bucket(fileName, bytes, s3Bucket, uploadRequest);

                var finalizeResponse = _bynderClient.FinalizeUpload(uploadRequest, chunkNumber);

                if (HasFinishedSuccessfully(finalizeResponse))
                {
                    var result = _bynderClient.SaveMedia(new SaveMediaQuery()
                    {
                        MediaId = (string)resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId)?.Data,
                        BrandId = brandId,
                        Filename = fileName,
                        ImportId = finalizeResponse.ImportId
                    });
                    resourceEntity.GetField(FieldTypeIds.ResourceBynderAssetId).Data = result.MediaId;
                }

                resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Done;
            }
            catch (Exception ex)
            {
                resourceEntity.GetField(FieldTypeIds.ResourceBynderUploadState).Data = BynderStates.Error;
                _inRiverContext.Logger.Log(LogLevel.Error, $"Error uploading resource entity {resourceEntity.Id}. Message: {ex.GetBaseException().Message}");
            }

            _inRiverContext.ExtensionManager.DataService.UpdateEntity(resourceEntity);
            _inRiverContext.Logger.Log(LogLevel.Information, $"Finished uploading resource entity {resourceEntity.Id}");
        }
        #endregion Methods
    }
}