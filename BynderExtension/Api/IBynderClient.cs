using System.Collections.Generic;

namespace Bynder.Api
{
    using Model;

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
        List<MetapropertyOption> GetMetapropertyOptions(string metapropertyId);
        List<Metaproperty> GetMetaproperties(List<string> metaPropertyIds = null);
        string DeleteMetapropertyOption(string metapropertyId, string metapropertyOptionId);
        string SaveAssetMetaproperties(string assetId, AssetMetapropertyList metapropertyList);

        string SaveAssetMetaproperties(string assetId, Dictionary<string, List<string>> metapropertyDictionary);

        void UploadPart(string s3Endpoint, string filename, byte[] buffer, int bytesRead, uint chunkNumber, UploadRequest uploadRequest, uint numberOfChunks);

        #endregion Methods
    }
}