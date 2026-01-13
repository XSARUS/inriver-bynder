using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            initialLoader.Execute(true);
        }

        #endregion Methods
    }
}