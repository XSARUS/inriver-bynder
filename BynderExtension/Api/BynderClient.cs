using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bynder.Api
{
    using Model;

    public class BynderClient : OAuthClient, IBynderClient
    {
        #region Fields
        //private const string PLUGIN_GETBYNDER_URI = "https://plugin.getbynder.com/";
        private readonly string _customerBynderUrl;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="settings"></param>
        public BynderClient(BynderClientSettings settings)
        {
            _customerBynderUrl = settings.CustomerBynderUrl;
            InitializeManager(settings.ConsumerKey, settings.ConsumerSecret, settings.Token, settings.TokenSecret);
        }
        #endregion Constructors

        #region Methods
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

        public Metaproperty GetMetadataProperty(string metaDataPropertyId)
        {
            var result = GetWithRetry($"{_customerBynderUrl}/api/v4/metaproperties/{metaDataPropertyId}/");
            return JsonConvert.DeserializeObject<Metaproperty>(result);
        }

        /// <summary>
        /// todo finish implementation of this method, now returns an empty list
        /// </summary>
        /// <param name="metaDataPropertyIds"></param>
        /// <returns></returns>
        public MetapropertyList GetMetadataProperties(List<string> metaDataPropertyIds = null)
        {
            string result;
            if(metaDataPropertyIds == null)
            {
                result = GetWithRetry($"{_customerBynderUrl}/api/v4/metaproperties/");
            }
            else
            {
                result = GetWithRetry($"{_customerBynderUrl}/api/v4/metaproperties/?ids={string.Join(",", metaDataPropertyIds)}");
            }

            //var ok =  JsonConvert.DeserializeObject<List<Metaproperty>>(result); //todo for later, not using it yet
            return new MetapropertyList();
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
        public FinalizeResponse FinalizeUpload(UploadRequest uploadRequest, uint chunkNumber)
        {
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("targetid", uploadRequest.S3File.TargetId),
                new KeyValuePair<string, string>("s3_filename",uploadRequest.S3Filename),
                new KeyValuePair<string, string>("chunks", chunkNumber.ToString())
            };

            string result = Post($"{_customerBynderUrl}/api/v4/upload/{uploadRequest.S3File.UploadId}/", postData);
            return JsonConvert.DeserializeObject<FinalizeResponse>(result);
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
        /// get asset download location
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        public AssetDownloadLocation GetAssetDownloadLocation(string assetId)
        {
            var apiResult = GetWithRetry($"{_customerBynderUrl}/api/v4/media/{assetId}/download/");
            return JsonConvert.DeserializeObject<AssetDownloadLocation>(apiResult);
        }
        public IList<BrandResponse> GetAvailableBranches()
        {
            string result = Get($"{_customerBynderUrl}/api/v4/brands/");
            return JsonConvert.DeserializeObject<IList<BrandResponse>>(result);
        }
        public string GetClosestS3Endpoint()
        {
            string result = Get($"{_customerBynderUrl}/api/upload/endpoint");
            return JsonConvert.DeserializeObject<string>(result);
        }
        public PollResponse PollStatus(IList<string> items)
        {
            var result = Get($"{_customerBynderUrl}/api/v4/upload/poll/?items={string.Join(",", items)}");
            return JsonConvert.DeserializeObject<PollResponse>(result);
        }
        public UploadRequest RequestUploadInformation(RequestUploadQuery requestUploadQuery)
        {
            if (requestUploadQuery == null || string.IsNullOrWhiteSpace(requestUploadQuery.Filename)) return null;
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("filename", requestUploadQuery.Filename)
            };

            string result = Post($"{_customerBynderUrl}/api/upload/init", postData);

            return (string.IsNullOrWhiteSpace(result)) ? null : JsonConvert.DeserializeObject<UploadRequest>(result);
        }
        public UploadResult SaveMedia(SaveMediaQuery saveMediaQuery)
        {
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("brandid",saveMediaQuery.BrandId),
                new KeyValuePair<string, string>("name", saveMediaQuery.Filename),
            };

            string uri;
            if (saveMediaQuery.MediaId == null)
            {
                uri = $"api/v4/media/save/{saveMediaQuery.ImportId}/";
            }
            else
            {
                uri = $"api/v4/media/{saveMediaQuery.MediaId}/save/{saveMediaQuery.ImportId}/";
            }

            string result = Post($"{_customerBynderUrl}/{uri}", postData);

            return (string.IsNullOrWhiteSpace(result)) ? null : JsonConvert.DeserializeObject<UploadResult>(result);
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
        public string SetMetaProperties(string assetId, Dictionary<string, List<string>> metapropertyDictionary)
        {
            // defence:
            if (string.IsNullOrWhiteSpace(assetId) || !metapropertyDictionary.Any()) return null;
            return SetMetaProperties(assetId, MetapropertyList.CreateFromDictionary(metapropertyDictionary));
        }

        public void UploadPart(string s3Endpoint, string filename, byte[] buffer, int bytesRead, uint chunkNumber, UploadRequest uploadRequest, uint numberOfChunks)
        {
            UploadPartToAmazon(filename, s3Endpoint, uploadRequest, chunkNumber, buffer, bytesRead, numberOfChunks);

            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("chunkNumber", chunkNumber.ToString()),
                new KeyValuePair<string, string>("targetid", uploadRequest.S3File.TargetId),
                new KeyValuePair<string, string>("filename", $"{uploadRequest.S3Filename}/p{chunkNumber}")
            };

            Post($"{_customerBynderUrl}/api/v4/upload/{uploadRequest.S3File.UploadId}/", postData);
        }
        public void UploadPartToAmazon(string filename, string awsBucket, UploadRequest uploadRequest, uint chunkNumber, byte[] fileContent, int numberOfBytes, uint numberOfChunks)
        {
            var finalKey = string.Format("{0}/p{1}", uploadRequest.MultipartParams.Key, chunkNumber);

            using (var client = new HttpClient())
            {
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(new StringContent(uploadRequest.MultipartParams.AWSAccessKeyid), "x-amz-credential");
                    formData.Add(new StringContent(finalKey), "key");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.Policy), "Policy");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.Signature), "X-Amz-Signature");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.Acl), "acl");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.Algorithm), "x-amz-algorithm");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.Date), "x-amz-date");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.SuccessActionStatus), "success_action_status");
                    formData.Add(new StringContent(uploadRequest.MultipartParams.ContentType), "Content-Type");
                    formData.Add(new StringContent(filename), "name");
                    formData.Add(new StringContent(chunkNumber.ToString()), "chunk");
                    formData.Add(new StringContent(numberOfChunks.ToString()), "chunks");
                    formData.Add(new StringContent(finalKey), "Filename");
                    formData.Add(new ByteArrayContent(fileContent, 0, numberOfBytes), "file");

                    var task = new Task(() =>
                    {
                        using (var response = client.PostAsync(awsBucket, formData))
                        {
                            response.Result.EnsureSuccessStatusCode();
                        }
                    });
                    task.Start();
                    Task.WaitAll(task);
                }
            }
        }
        #endregion Methods
    }
}