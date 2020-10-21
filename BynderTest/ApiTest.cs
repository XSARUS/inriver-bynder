using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass]
    public class ApiTest : TestBase
    {
        private const string _testAssetId = "CE9F3C36-3EDF-4F92-979A791B83B21DDA";
        private const string _testIntegrationId = "41a92562-bfd9-4847-a34d-4320bcef5e4a";
        private const string _testMetaPropertyId = "EE9827C8-0AA6-4334-AFF1708DA0A67A52";

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
                new Metaproperty{ Id="50B5233E-AD1C-4CF5-82B910BADA62F30F", Value="Hello" },
                new Metaproperty{ Id="C284234B-29B6-4CA8-B907B728455F30EA", Value = "World" }
            };
            var result = bynderBynderClient.SetMetaProperties(_testAssetId, mpl);
            Logger.Log(result);
        }

        [TestMethod]
        public void GetAssetByAssetId()
        {
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            Asset asset = bynderClient.GetAssetByAssetId(_testAssetId);

            var originalFileName = asset.GetOriginalFileName();
            Logger.Log(originalFileName);

            Assert.AreNotEqual(string.Empty, originalFileName, "Got no result");
        }

        [TestMethod]
        public void GetMetaproperties()
        {
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            var metaproperty = bynderClient.GetMetadataProperty(_testMetaPropertyId);
            Assert.IsNotNull(metaproperty?.Name);

            // todo not using it yet
            //var metaproperties = bynderClient.GetMetadataProperties(new List<string> { _testMetaPropertyId });
            //Assert.IsNotNull(metaproperties);
            //Assert.AreEqual(1, metaproperties.Count);
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
