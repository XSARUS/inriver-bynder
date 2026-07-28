using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [Ignore("Only use for debugging")]
    [TestClass]
    public class UploaderTest : TestBase
    {

        [TestMethod]
        public void TestUpload()
        {
            var uploader = new Uploader
            {
                Context = InRiverContext
            };

            uploader.Context.Settings = TestSettings;
            uploader.EntityUpdated(232963, new string[] { });
        }
    }
}
