using Bynder.Api.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BynderTest
{
    [TestClass]
    public class JsonConvertTest
    {
        #region Methods

        [TestMethod]
        public void TestConverion()
        {
            string assetApiJson = "";
            Asset assetObj = JsonConvert.DeserializeObject<Asset>(assetApiJson);
            //Assert.AreEqual(12, assetObj.MetaProperties.Count);

            var assetString = JsonConvert.SerializeObject(assetObj);

            string assetWithMetaproperties = "";
            Assert.AreEqual(assetWithMetaproperties, assetString);
        }

        #endregion Methods
    }
}