using Bynder.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BynderTest
{
    [TestClass]
    public class JsonConvertTest
    {
        #region Methods

        [TestMethod,Ignore("Depends on previous code-base with the custom API!")]
        public void TestConversion()
        {
            /*string assetApiJson = "";
            Asset assetObj = JsonConvert.DeserializeObject<Asset>(assetApiJson);
            //Assert.AreEqual(12, assetObj.MetaProperties.Count);

            var assetString = JsonConvert.SerializeObject(assetObj);

            string assetWithMetaproperties = "";
            Assert.AreEqual(assetWithMetaproperties, assetString);*/
        }

        [TestMethod]
        public void TestFilenameExtensionMediatypeMappingSerialization()
        {
            var mappings = new Dictionary<string, List<MediaTypeTransformConfig>>
            {
                ["tif"] = new List<MediaTypeTransformConfig>
                {
                    new MediaTypeTransformConfig { MediaType = "webimage",  FilenameRegex = "idk" },
                    new MediaTypeTransformConfig { MediaType = "ecommerce", FilenameRegex = "idk" },
                },
                ["idk"] = new List<MediaTypeTransformConfig>
                {
                    new MediaTypeTransformConfig { MediaType = "webimage", FilenameRegex = "idk" },
                }
            };

            var json = JsonConvert.SerializeObject(mappings, Formatting.Indented);

            Assert.AreNotEqual(json, string.Empty);
        }

        #endregion Methods
    }
}