using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bynder.Extension;

namespace BynderTest
{
    [TestClass]
    public class WorkerTest : TestBase
    {
        [Ignore("Add resource entityId here")]
        [DataRow(123)]
        [DataTestMethod]
        public void TestEntityCreate(int entityId)
        {
            var worker = new Worker
            {
                Context = InRiverContext
            };
            worker.Context.Settings = TestSettings;
            worker.EntityCreated(entityId);
            Logger.Log("Done!");

        }

        [Ignore("Add product entityId here or adjust the test with an other field for other entitytype")]
        [DataRow(123)]
        [DataTestMethod]
        public void TestEntityUpdate(int entityId)
        {
            var worker = new Worker
            {
                Context = InRiverContext
            };
            worker.Context.Settings = TestSettings;

            string[] fields = {
                "ProductName"
            };
            worker.EntityUpdated(entityId, fields);
            Logger.Log("Done!");
        }
    }
}
