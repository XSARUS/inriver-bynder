using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace BynderTest
{
    [TestClass, Ignore("Only run manually")]
    public class AssetLoaderTest : TestBase
    {
        #region Methods

        [TestMethod]
        public void TestAssetLoader()
        {
            var initialLoader = new Bynder.Extension.AssetLoader
            {
                Context = InRiverContext
            };
            initialLoader.Context.Settings = TestSettings;
            string testResult = initialLoader.Test();

            var forbidden = new[] { "error", "false" };

            Assert.IsFalse(
                forbidden.Any(testResult.Contains),
                $"Result contains one of the forbidden values: {string.Join(", ", forbidden)}"
            );

            initialLoader.Execute(true);
        }

        #endregion Methods
    }
}