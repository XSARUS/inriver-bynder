using Bynder.Api.Model;
using System.Collections.Generic;

namespace Bynder.Api
{
    public interface IBynderClient
    {
        #region Methods

        string CreateAssetUsage(string assetId, string integrationId, string resourceUrl = null);

        string DeleteAssetUsage(string assetId, string integrationId, string resourceUrl = null);

        byte[] DownloadAsset(string assetId);

        FinalizeResponse FinalizeUpload(UploadRequest uploadRequest, uint chunkNumber);

        Account GetAccount();

        Asset GetAssetByAssetId(string assetId);

        AssetCollection GetAssetCollection(string query, int page = 1, int limit = 10);

        AssetDownloadLocation GetAssetDownloadLocation(string assetId);

        IList<BrandResponse> GetAvailableBranches();

        string GetClosestS3Endpoint();

        PollResponse PollStatus(IList<string> items);

        UploadRequest RequestUploadInformation(RequestUploadQuery requestUploadQuery);

        UploadResult SaveMedia(SaveMediaQuery saveMediaQuery);

        string SetMetaProperties(string assetId, MetapropertyList metapropertyList);

        string SetMetaProperties(string assetId, Dictionary<string, List<string>> metapropertyDictionary);

        void UploadPart(string s3Endpoint, string filename, byte[] buffer, int bytesRead, uint chunkNumber, UploadRequest uploadRequest, uint numberOfChunks);

        #endregion Methods
    }
}