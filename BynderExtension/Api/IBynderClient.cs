using System.Collections.Generic;

namespace Bynder.Api
{
    using Model;

    public interface IBynderClient
    {

        #region Methods

        string CreateAssetUsage(string assetId, string integrationId, string resourceUrl = null);

        string DeleteAssetUsage(string assetId, string integrationId, string resourceUrl = null);

        string DeleteMetapropertyOption(string metapropertyId, string metapropertyOptionId);

        byte[] DownloadAsset(string assetId);

        FinalizeResponse FinalizeUpload(UploadRequest uploadRequest, uint chunkNumber);

        Account GetAccount();

        Asset GetAssetByAssetId(string assetId);

        AssetCollection GetAssetCollection(string query, int page = 1, int limit = 10);

        AssetDownloadLocation GetAssetDownloadLocation(string assetId);

        IList<BrandResponse> GetAvailableBranches();

        string GetClosestS3Endpoint();

        List<Metaproperty> GetMetaproperties(List<string> metaPropertyIds = null);

        List<MetapropertyOption> GetMetapropertyOptions(string metapropertyId);

        PollResponse PollStatus(IList<string> items);

        UploadRequest RequestUploadInformation(RequestUploadQuery requestUploadQuery);

        string SaveAssetMetaproperties(string assetId, AssetMetapropertyList metapropertyList);

        string SaveAssetMetaproperties(string assetId, Dictionary<string, List<string>> metapropertyDictionary);

        UploadResult SaveMedia(SaveMediaQuery saveMediaQuery);
        string SaveMetapropertyOption(string metapropertyId, MetapropertyOptionPost metapropertyOption);
        void UploadPart(string s3Endpoint, string filename, byte[] buffer, int bytesRead, uint chunkNumber, UploadRequest uploadRequest, uint numberOfChunks);

        #endregion Methods

    }
}