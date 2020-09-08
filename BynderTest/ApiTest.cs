using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass]
    public class ApiTest : TestBase
    {
        private const string _testAssetId = "9542A933-2DF5-4999-9AB52701F33613C0";
        private const string _testIntegrationId = "41a92562-bfd9-4847-a34d-4320bcef5e4a";

        private BynderClientSettings _bynderSettings;
                
        [TestInitialize]
        public void Init()
        {
            _bynderSettings = new BynderClientSettings()
            {
                ConsumerKey = TestSettings[SettingNames.ConsumerKey],
                ConsumerSecret = TestSettings[SettingNames.ConsumerSecret],
                CustomerBynderUrl = TestSettings[SettingNames.CustomerBynderUrl],
                Token = TestSettings[SettingNames.Token],
                TokenSecret = TestSettings[SettingNames.TokenSecret]
            };
        }

        [TestMethod]
        public void TestFlow()
        {
            GetAccount();
            CreateAssetUsage();
            GetAssetByAssetId();
            PostMetaProperties();
            GetAssetCollection();
            DeleteAssetUsage();
        }

        [Ignore("Add valid entity id here")]
        [DataRow(123)]
        [DataTestMethod]
        public void UploadEntityTest(int entityId)
        {
            Uploader uploader = new Uploader() { Context = InRiverContext };
            uploader.Context.Settings = TestSettings;
            uploader.EntityUpdated(entityId, null);
        }

        public void CreateAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var result = bynderBynderClient.CreateAssetUsage(_testAssetId, _testIntegrationId, "http://test.com/123");
            Logger.Log(result);
        }

        public void DeleteAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var result = bynderBynderClient.DeleteAssetUsage(_testAssetId, _testIntegrationId);
            Logger.Log(result);
        }

        public void PostMetaProperties()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var mpl = new MetapropertyList()
            {
                new Metaproperty("50B5233E-AD1C-4CF5-82B910BADA62F30F", "Hello"),
                new Metaproperty("C284234B-29B6-4CA8-B907B728455F30EA", "World")
            };
            var result = bynderBynderClient.SetMetaProperties(_testAssetId, mpl);
            Logger.Log(result);
        }

        public void GetAssetByAssetId() { 
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            Asset asset = bynderClient.GetAssetByAssetId(_testAssetId);
            var originalFileName = asset.GetOriginalFileName();
            Logger.Log(originalFileName);

            Assert.AreNotEqual(string.Empty, originalFileName, "Got no result");
        }

        public void GetAccount()
        {
           BynderClient bynderClient = new BynderClient(_bynderSettings);
            var accountName = bynderClient.GetAccount().Name;

            Logger.Log(accountName);
            Assert.IsNotNull(accountName);
        }

        public void GetAssetCollection()
        {
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            var collection = bynderClient.GetAssetCollection("");
            Assert.IsInstanceOfType(collection, typeof(AssetCollection));
            Logger.Log("Total assets in result: " + collection.Total.Count);
        }
    }
}
