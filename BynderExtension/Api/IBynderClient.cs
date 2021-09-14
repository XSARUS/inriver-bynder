using Bynder.Api.Model;
using System.Collections.Generic;

namespace Bynder.Api
{
    public interface IBynderClient
    {
        string CreateAssetUsage(string assetId, string integrationId, string resourceUrl = null);
        string DeleteAssetUsage(string assetId, string integrationId, string resourceUrl = null);
        byte[] DownloadAsset(string assetId);
        Account GetAccount();
        Asset GetAssetByAssetId(string assetId);
        AssetCollection GetAssetCollection(string query, int page = 1, int limit = 10);
        AssetDownloadLocation GetAssetDownloadLocation(string assetId);
        string SetMetaProperties(string assetId, MetapropertyList metapropertyList);
        string SetMetaProperties(string assetId, Dictionary<string, List<string>> metapropertyDictionary);
        string GetClosestS3Endpoint();
        UploadRequest RequestUploadInformation(RequestUploadQuery requestUploadQuery);
        void UploadPart(string s3Endpoint, string filename, byte[] buffer, int bytesRead, uint chunkNumber, UploadRequest uploadRequest, uint numberOfChunks);
        FinalizeResponse FinalizeUpload(UploadRequest uploadRequest, uint chunkNumber);
        PollResponse PollStatus(IList<string> items);
        UploadResult SaveMedia(SaveMediaQuery saveMediaQuery);
        IList<BrandResponse> GetAvailableBranches();
    }
}