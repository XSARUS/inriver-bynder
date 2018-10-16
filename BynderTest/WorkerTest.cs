using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bynder.Extension;

namespace BynderTest
{
    [TestClass]
    public class WorkerTest : TestBase
    {
        [TestMethod]
        public void TestEntityCreate()
        {
            var worker = new Worker
            {
                Context = InRiverContext
            };
            worker.Context.Settings = TestSettings;
            worker.EntityCreated(4016);
            Logger.Log("Done!");

        }

        [TestMethod]
        public void TestEntityUpdate()
        {
            var worker = new Worker
            {
                Context = InRiverContext
            };
            worker.Context.Settings = TestSettings;

            string[] fields = {
                "ProductName"
            };
            worker.EntityUpdated(4036, fields);
            Logger.Log("Done!");
        }
    }
}
