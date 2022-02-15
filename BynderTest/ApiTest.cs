using Bynder.Api;
using Bynder.Api.Model;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BynderTest
{
    [TestClass]
    public class ApiTest : TestBase
    {
        #region Fields

        private const string _testAssetId = "86020C4E-9140-4713-9356AB72024CD0D7";
        private const string _testIntegrationId = "41a92562-bfd9-4847-a34d-4320bcef5e4a";
        private const string _testMetaPropertyId = "EE9827C8-0AA6-4334-AFF1708DA0A67A52";

        #endregion Fields

        #region Methods

        public void CreateAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(BynderSettings);
            var result = bynderBynderClient.CreateAssetUsage(_testAssetId, _testIntegrationId, "http://test.com/123");
            Logger.Log(result);
        }

        public void DeleteAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(BynderSettings);
            var result = bynderBynderClient.DeleteAssetUsage(_testAssetId, _testIntegrationId);
            Logger.Log(result);
        }

        public void GetAccount()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            var accountName = bynderClient.GetAccount().Name;

            Logger.Log(accountName);
            Assert.IsNotNull(accountName);
        }

        [TestMethod]
        public void GetAssetByAssetId()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            Asset asset = bynderClient.GetAssetByAssetId(_testAssetId);

            var originalFileName = asset.GetOriginalFileName();
            Logger.Log(originalFileName);

            Assert.AreNotEqual(string.Empty, originalFileName, "Got no result");
        }

        public void GetAssetCollection()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            var collection = bynderClient.GetAssetCollection("");
            Assert.IsInstanceOfType(collection, typeof(AssetCollection));
            Logger.Log("Total assets in result: " + collection.Total.Count);
        }

        [TestMethod, Ignore("currently only used to retreive the metaproperties in the bynderclient")]
        public void GetMetaproperties()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            var metaproperties = bynderClient.GetMetadataProperties();
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetMetaproperty()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            var metaproperty = bynderClient.GetMetadataProperty(_testMetaPropertyId);
            Assert.IsNotNull(metaproperty?.Name);
        }

        [TestMethod]
        public void PostMetaProperties()
        {
            BynderClient bynderClient = new BynderClient(BynderSettings);
            var mpl = new MetapropertyList()
            {
                new Metaproperty{ Id="4F1C2956-01DC-415C-94BB1D770FEE5A98", Values = new List<string>{ "Hello" } },
                new Metaproperty{ Id="ABFC192D-A92B-47A0-9AFE96BBCBA3E79A", Values = new List<string>{ "bci", "gnr" } }
            };
            var result = bynderClient.SetMetaProperties(_testAssetId, mpl);
            Logger.Log(result);
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

        #endregion Methods
    }
}