using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace BynderTest
{
    [TestClass, Ignore("Only run manually")]
    public class AssetLoaderTest : TestBase
    {
        #region Fields

        private AssetLoader _extension;

        #endregion Fields

        #region Methods

        [TestMethod, Ignore("Only use for debugging!")]
        public void GetMediaTest()
        {
            var worker = _extension.GetWorker();
            var media = worker.GetMedia("B8ED3B94-21A3-42C3-B10D254DE248795F");
            Logger.Log($"Media found: {media.Id}");
        }

        [TestInitialize]
        public void Init()
        {
            _extension = new AssetLoader
            {
                Context = InRiverContext
            };

            _extension.Context.Settings = TestSettings;
        }

        [TestMethod, Ignore("Only use for debugging!")]
        public void TestAssetLoaderExecution()
        {
            _extension.Execute(true);
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

        #endregion Methods
    }
}