using System.Collections.Generic;
using System.Linq;
using System.Net;
using Bynder.Api.Model;
using Newtonsoft.Json;

namespace Bynder.Api
{
    public class BynderClient : OAuthClient, IBynderClient
    {
        private readonly string _customerBynderUrl;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="settings"></param>
        public BynderClient(BynderClientSettings settings)
        {
            _customerBynderUrl = settings.CustomerBynderUrl;
            InitializeManager(settings.ConsumerKey, settings.ConsumerSecret, settings.Token, settings.TokenSecret);
        }

        /// <summary>
        /// get account information
        /// </summary>
        /// <returns></returns>
        public Account GetAccount()
        {
            var apiResult = Get($"{_customerBynderUrl}/api/v4/account/");
            return JsonConvert.DeserializeObject<Account>(apiResult);
        }

        /// <summary>
        /// get asset by asset Id
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public Asset GetAssetByAssetId(string assetId)
        {
            var apiResult = GetWithRetry($"{_customerBynderUrl}/api/v4/media/{assetId}/?versions=1");
            return JsonConvert.DeserializeObject<Asset>(apiResult);
        }

        /// <summary>
        /// get asset collection by query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public AssetCollection GetAssetCollection(string query, int page = 1, int limit = 10)
        {
            // modify/explode query to override pagination
            query += $"&page={page}&limit={limit}&total=1";
            var apiResult = Get($"{_customerBynderUrl}/api/v4/media/?" + query);
            AssetCollection collection = JsonConvert.DeserializeObject<AssetCollection>(apiResult);
            collection.Page = page;
            collection.Limit = limit;
            return collection;
        }

        /// <summary>
        /// download asset
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public byte[] DownloadAsset(string assetId)
        {
            var assetDownloadLocation = GetAssetDownloadLocation(assetId);
            return new WebClient().DownloadData(assetDownloadLocation.S3_File);
        }

        /// <summary>
        /// get asset download location
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public AssetDownloadLocation GetAssetDownloadLocation(string assetId)
        {
            var apiResult = GetWithRetry($"{_customerBynderUrl}/api/v4/media/{assetId}/download/");
            return JsonConvert.DeserializeObject<AssetDownloadLocation>(apiResult);
        }

        /// <summary>
        /// set meta properties for asset
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="metapropertyList"></param>
        /// <returns></returns>
        public string SetMetaProperties(string assetId, MetapropertyList metapropertyList)
        {
            return Post($"{_customerBynderUrl}/api/v4/media/{assetId}/", metapropertyList.GetPostData());
        }

        /// <summary>
        /// set meta properties for asset
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="metapropertyDictionary"></param>
        /// <returns></returns>
        public string SetMetaProperties(string assetId, Dictionary<string, string> metapropertyDictionary)
        {
            // defence:
            if (string.IsNullOrWhiteSpace(assetId) || !metapropertyDictionary.Any()) return null;
            return SetMetaProperties(assetId, MetapropertyList.CreateFromDictionary(metapropertyDictionary));
        }

        /// <summary>
        /// delete asset usage
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="integrationId"></param>
        /// <param name="resourceUrl"></param>
        /// <returns></returns>
        public string DeleteAssetUsage(string assetId, string integrationId, string resourceUrl = null)
        {
            if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(integrationId)) return null;
            string deleteUrl = $"{_customerBynderUrl}/api/media/usage/?asset_id={assetId}&integration_id={integrationId}";
            if (!string.IsNullOrWhiteSpace(resourceUrl)) deleteUrl += resourceUrl;
            return Delete(deleteUrl);
        }

        /// <summary>
        /// create asset usage
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="integrationId"></param>
        /// <param name="resourceUrl"></param>
        /// <returns></returns>
        public string CreateAssetUsage(string assetId, string integrationId, string resourceUrl = null)
        {
            if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(integrationId)) return string.Empty;
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("asset_id", assetId),
                new KeyValuePair<string, string>("integration_id", integrationId)
            };
            if (!string.IsNullOrWhiteSpace(resourceUrl))
            {
                postData.Add(new KeyValuePair<string, string>("uri", resourceUrl));
            }
            return Post($"{_customerBynderUrl}/api/media/usage", postData);
        }
    }
}