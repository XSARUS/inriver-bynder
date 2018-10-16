using System.Collections.Generic;
using Bynder.Api.Model;

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
        string SetMetaProperties(string assetId, Dictionary<string, string> metapropertyDictionary);
    }
}