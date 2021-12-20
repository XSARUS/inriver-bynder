using Bynder.Api;
using Bynder.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass]
    public class WorkerTest : TestBase
    {
        [Ignore("Test create/update for asset")]
        [DataTestMethod, DataRow("CE9F3C36-3EDF-4F92-979A791B83B21DDA")]
        public void TestAssetUpdatedWorker(string bynderAssetId)
        {
            InRiverContext.Settings = TestSettings;
            var worker = new Bynder.Workers.AssetUpdatedWorker(InRiverContext, new BynderClient(BynderSettings), new Bynder.Utils.FilenameEvaluator(InRiverContext));
            var updaterResult = worker.Execute(bynderAssetId, Bynder.Enums.NotificationType.DataUpsert);
        }

        [Ignore("Add resource entityId here")]
        [DataTestMethod, DataRow(123)]
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
        [DataTestMethod, DataRow(123)]
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
