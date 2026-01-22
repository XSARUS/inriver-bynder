using Bynder.Config;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace BynderTest
{
    [TestClass, Ignore("Only run manually")]
    public class AssetLoaderTest : TestBase
    {
        private AssetLoader _extension;

        #region Methods
        [TestInitialize]
        public void Init()
        {
            _extension = new AssetLoader
            {
                Context = InRiverContext
            };
            
            _extension.Context.Settings = TestSettings;
        }

        [TestMethod]
        public void TestAssetLoaderTestMethod()
        {
            string testResult = _extension.Test();

            var forbidden = new[] { "error", "false" };

            Assert.IsFalse(
                forbidden.Any(testResult.Contains),
                $"Result contains one of the forbidden values: {string.Join(", ", forbidden)}"
            );

            Logger.Log($"Test result: {testResult}");
        }

        [TestMethod, Ignore("Only use for debugging!")]
        public void TestAssetLoaderExecution()
        {
            _extension.Execute(true);
        }

        #endregion Methods
    }
}