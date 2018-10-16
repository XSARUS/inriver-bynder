using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bynder.Api;
using Bynder.Api.Model;

namespace BynderTest
{
    [TestClass]
    public class ApiTest : TestBase
    {
        private readonly BynderClientSettings _bynderSettings = new BynderClientSettings()
        {
            ConsumerKey = "5B9BDF90-65C8-4122-B1BAED1CDE643482",
            ConsumerSecret = "0f5eed26ae737f2e30e6a91702875f5f",
            CustomerBynderUrl = "https://xsarus.getbynder.com",
            Token = "DE126C3E-81B8-442E-9FFF66A0A85732F5",
            TokenSecret = "69badb73a05e05fb730c4b86bab57bd9"
        };

        [TestMethod]
        public void CreateAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var result = bynderBynderClient.CreateAssetUsage("9542A933-2DF5-4999-9AB52701F33613C0", "41a92562-bfd9-4847-a34d-4320bcef5e4a", "http://test.com/123");
            Logger.Log(result);
        }

        [TestMethod]
        public void DeleteAssetUsage()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var result = bynderBynderClient.DeleteAssetUsage("9542A933-2DF5-4999-9AB52701F33613C0", "41a92562-bfd9-4847-a34d-4320bcef5e4a");
            Logger.Log(result);
        }

        [TestMethod]
        public void PostMetaProperties()
        {
            BynderClient bynderBynderClient = new BynderClient(_bynderSettings);
            var mpl = new MetapropertyList()
            {
                new Metaproperty("50B5233E-AD1C-4CF5-82B910BADA62F30F", "Hallo"),
                new Metaproperty("C284234B-29B6-4CA8-B907B728455F30EA", "World")
            };
            var result = bynderBynderClient.SetMetaProperties("9542A933-2DF5-4999-9AB52701F33613C0", mpl);
            Logger.Log(result);
        }

        [TestMethod]
        public void GetAssetByAssetId() { 
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            Asset asset = bynderClient.GetAssetByAssetId("9542A933-2DF5-4999-9AB52701F33613C0");
            Logger.Log(asset.GetOriginalFileName());
            Assert.AreNotEqual(string.Empty, asset.GetOriginalFileName(), "Got no result");
        }

        [TestMethod]
        public void GetAccount()
        {
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            Logger.Log(bynderClient.GetAccount().Name);
        }

        [TestMethod]
        public void GetAssetCollection()
        {
            BynderClient bynderClient = new BynderClient(_bynderSettings);
            var collection = bynderClient.GetAssetCollection("");
            Assert.IsInstanceOfType(collection, typeof(AssetCollection));
            Logger.Log("Total assets in result: " + collection.Total.Count);
        }
    }
}
