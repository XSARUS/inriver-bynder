using Bynder.Api;
using Bynder.Extension;
using Bynder.Sdk.Model;
using Bynder.Sdk.Settings;
using Bynder.Utils.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using SdkIBynderClient = Bynder.Sdk.Service.BynderClient;

namespace BynderTest
{
    //[Ignore("Only use for debugging!")]
    [TestClass]
    public class WorkerTest : TestBase
    {
        #region Methods

        [Ignore("Test create/update for asset")]
        [TestMethod, DataRow("928353AB-D2FE-4878-83C77D54A6C3EEBA")]
        public void TestAssetUpdatedWorker(string bynderAssetId)
        {
            InRiverContext.Settings = TestSettings;

            var bynderClientSettings = BynderClientSettings.Create(TestSettings);
            var configuration = new Configuration()
            {
                BaseUrl = new Uri(bynderClientSettings.CustomerBynderUrl),
                ClientId = bynderClientSettings.ConsumerKey,
                ClientSecret = bynderClientSettings.ConsumerSecret
            };
            var worker = new Bynder.Workers.AssetUpdatedWorker(
                InRiverContext,
                new Bynder.Utils.FilenameEvaluator(InRiverContext),
                new SdkIBynderClient(configuration)
            );
            var updaterResult = worker.Execute(bynderAssetId, Bynder.Enums.NotificationType.DataUpsert);
        }

        [Ignore("Add resource entityId here")]
        [TestMethod, DataRow(123)]
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
        [TestMethod, DataRow(123)]
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

        [Ignore()]
        [TestMethod, DataRow("A4DDC8EC-C6FA-44FE-9AEC98D4B4C7A9A9" /* PON */)]
        public void TestEntityFilename(string bynderAssetId)
        {
            InRiverContext.Settings = TestSettings;

            var worker = new Bynder.Workers.AssetUpdatedWorker(
                InRiverContext,
                new Bynder.Utils.FilenameEvaluator(InRiverContext),
                _bynderClient
            );

            Media media = worker.GetMedia(bynderAssetId);
            Console.WriteLine($"Filename: {media.GetOriginalFileName()}");

            var (url, filename) = MediaHelper.GetDownloadUrlAndFilename(InRiverContext, _bynderClient, media).GetAwaiter().GetResult();
            Console.WriteLine($"Url: {url} >> Filename: {filename}");
        }

        #endregion Methods
    }
}