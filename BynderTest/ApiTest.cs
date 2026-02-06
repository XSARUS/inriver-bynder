using Bynder.Extension;
using Bynder.Sdk.Enums;
using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Asset;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BynderTest
{
    [TestClass, Ignore("Only run manually")]
    public class ApiTest : TestBase
    {
        #region Fields

        private const string _testAssetId = "***";
        private const string _testIntegrationId = "***";

        private Dictionary<string, string> createdObjects = new Dictionary<string, string>();

        #endregion Fields

        #region Methods

        /// <summary>
        /// You need to have an Integration Id.
        /// Get one at: https://{subdomain}.getbynder.com/pysettings/#integrations/all
        /// </summary>
        public void CreateAssetUsage()
        {
            var query = new Bynder.Sdk.Query.Asset.AssetUsageQuery(_testIntegrationId, _testAssetId)
            {
                Uri = "http://test.com/123"
            };
            var result = _bynderClient.GetAssetService().CreateAssetUsage(query).GetAwaiter().GetResult();
            Logger.Log($"{result.StatusCode}: {result.Message}");
        }

        /// <summary>
        /// You need to have an Integration Id.
        /// Get one at: https://{subdomain}.getbynder.com/pysettings/#integrations/all
        /// </summary>
        public void DeleteAssetUsage()
        {
            var query = new Bynder.Sdk.Query.Asset.AssetUsageQuery(_testIntegrationId, _testAssetId);
            var result = _bynderClient.GetAssetService().DeleteAssetUsage(query).GetAwaiter().GetResult();
            Logger.Log($"{result.StatusCode}: {result.Message}");
        }

        [Ignore("Only manual!")]
        [TestMethod]
        [DataRow("5A9F4A52-1F5B-421A-94DF9249CA177926")]
        public void DeleteMetaproperty(string metapropertyId)
        {
            var delResult = _bynderClient.GetAssetService().DeleteMetapropertyAsync(metapropertyId).GetAwaiter().GetResult();
            Logger.Log($"Metaproperty {metapropertyId} deleted-status: " + delResult?.StatusCode.ToString());
        }

        [Ignore("Only manual!")]
        [TestMethod]
        [DataRow("5A9F4A52-1F5B-421A-94DF9249CA177926", "F8732D3E-7BF5-4F12-97D81570E019EDC2")]
        public void DeleteMetapropertyOption(string metapropertyId, string optionId)
        {
            var delOptionResult = _bynderClient.GetAssetService().DeleteMetapropertyOptionAsync(metapropertyId, optionId).GetAwaiter().GetResult();
            Logger.Log($"Metaproperty-option {optionId} for metaproperty {metapropertyId} deleted-status: " + delOptionResult?.StatusCode.ToString());
        }

        public void GetAccount()
        {
            // Feedback from call in Postman: Insufficient scope, required: current.user:read
            User user = _bynderClient.GetUserService().GetCurrentUserAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(user);
            Assert.IsNotNull(user.Email);

            Logger.Log(user.Email);
        }

        [TestMethod, Ignore("Do this manually if you want to test an asset on itself")]
        public void GetAssetByAssetId()
        {
            Assert.IsNotEmpty(_testAssetId);
            Media media = _bynderClient.GetAssetService().GetAssetByMediaQuery(_testAssetId).GetAwaiter().GetResult();

            var originalFileName = media.GetOriginalFileName();
            Logger.Log(originalFileName);

            Assert.AreNotEqual(string.Empty, originalFileName, "Got no result");
        }

        public void GetAssetCollection()
        {
            var query = new Bynder.Sdk.Query.Collection.GetCollectionsQuery()
            {
                Limit = 1,
                MinCount = 1,
            };
            var oneRandomCollection = _bynderClient.GetCollectionService().GetCollectionsAsync(query).GetAwaiter().GetResult();

            Assert.IsNotEmpty(oneRandomCollection);

            var specificCollection = oneRandomCollection.FirstOrDefault();

            Assert.IsNotNull(specificCollection);

            var collection = _bynderClient.GetCollectionService().GetCollectionAsync(specificCollection.Id).GetAwaiter().GetResult();
            // Note: as seen in the Bynder Postman calls, a collection retrieve by this call does not contain the Id or the MediaCount
            Assert.IsInstanceOfType(collection, typeof(Bynder.Sdk.Model.Collection));

            // To get the asset Ids of a collection you should do this
            var mediaQuery = new Bynder.Sdk.Query.Collection.GetMediaQuery(specificCollection.Id);
            var collectionAssetIds = _bynderClient.GetCollectionService().GetMediaAsync(mediaQuery).GetAwaiter().GetResult();

            Assert.IsNotNull(collectionAssetIds);
            Assert.HasCount(specificCollection.MediaCount, collectionAssetIds);

            Logger.Log("Total assets/media in result: " + collectionAssetIds.Count);
        }

        [TestMethod]
        public void GetMetaproperties()
        {
            var metaproperties = _bynderClient.GetAssetService().GetMetapropertiesAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(metaproperties);
        }

        [TestMethod]
        public void GetMetaproperty()
        {
            var metaproperties = _bynderClient.GetAssetService().GetMetapropertiesAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(metaproperties);

            var metaPropertyId = metaproperties.FirstOrDefault().Value.Id;
            var query = new Bynder.Sdk.Query.Asset.MetapropertyQuery(metaPropertyId);
            var metaproperty = _bynderClient.GetAssetService().GetMetapropertyAsync(query).GetAwaiter().GetResult();

            Assert.IsNotNull(metaproperty);
            Assert.IsNotNull(metaproperty?.Name);
        }

        [TestMethod]
        public void GetMetapropertyOptions()
        {
            var metaproperties = _bynderClient.GetAssetService().GetMetapropertiesAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(metaproperties);

            var metaPropertyId = metaproperties.FirstOrDefault().Value.Id;
            var metaproperty = _bynderClient.GetAssetService().GetMetapropertyAsync(
                new Bynder.Sdk.Query.Asset.MetapropertyQuery(metaPropertyId)
            ).GetAwaiter().GetResult();

            Assert.IsNotNull(metaproperty);
            Assert.IsNotNull(metaproperty?.Name);

            var options = _bynderClient.GetAssetService().GetMetapropertyOptionsAsync(metaPropertyId, new MetapropertyOptionQuery());
            Assert.IsNotNull(options);
        }

        [TestMethod]
        public void PostAssetMetaproperties()
        {
            var query = new ModifyMediaQuery(_testAssetId);
            query.AddMetapropertyOptions("4F1C2956-01DC-415C-94BB1D770FEE5A98", new List<string> { "Hello" });
            query.AddMetapropertyOptions("ABFC192D-A92B-47A0-9AFE96BBCBA3E79A", new List<string> { "bci", "gnr" });

            var result = _bynderClient.GetAssetService().ModifyMediaAsync(query).GetAwaiter().GetResult();

            Assert.IsNotNull(result);
            Assert.AreEqual(202, result.StatusCode);
            Logger.Log($"Metaproperties and options set for asset / media {_testAssetId}");
        }

        [TestMethod]
        [DataRow(false)]
        public void PostMetapropertyOptions(bool internalCall)
        {
            var guid = Guid.NewGuid();

            // Create a metaproperty to test with
            var metaproperty = new Bynder.Sdk.Model.Metaproperty()
            {
                Type = MetapropertyType.Select,
                IsRequired = false,
                IsFilterable = false,
                Label = "API TEST metaproperty" + guid,
                Name = "ApiTestMetaProperty" + guid,
                ZIndex = 100,
                IsEditable = true,
                IsMainfilter = false,
                IsMultiSelect = true,
            };
            // why do I get: name already registered for mp: 9be5c51b-51cf-45dd-8dd9-ff9b44d535a8
            var result = _bynderClient.GetAssetService().UpsertMetapropertyAsync(metaproperty).GetAwaiter().GetResult();
            Assert.IsNotNull(result);
            Assert.AreNotEqual(string.Empty, result.Id);
            Assert.AreNotEqual("00000000-0000-0000-0000000000000000", result.Id);
            Assert.AreEqual(201, result.StatusCode);

            if (internalCall) createdObjects.Add("mp", result.Id);

            // The actual test
            var option = new Bynder.Sdk.Model.MetapropertyOption()
            {
                Name = "Test_optie" + guid,
                Label = "Test_optie " + guid,
                Labels = new Dictionary<string, string>
                {
                    { "nl_NL", "Test_optie" },
                    { "de_DE", "Test_option" },
                    { "fr_FR", "Test_option" },
                    { "it_IT", "Test_opzione" }
                },
                ZIndex = 5.ToString(),
                IsSelectable = true,
            };
            var optionResult = _bynderClient.GetAssetService().UpsertMetapropertyOptionAsync(result.Id, option).GetAwaiter().GetResult();
            Assert.IsNotNull(optionResult);
            Assert.AreEqual(201, optionResult.StatusCode);

            if (internalCall) createdObjects.Add("option", optionResult.Id);

            // Remove the created metaproperty + option
            if (internalCall) return;

            var delOptionResult = _bynderClient.GetAssetService().DeleteMetapropertyOptionAsync(result.Id, optionResult.Id).GetAwaiter().GetResult();
            Logger.Log($"Metaproperty-option {optionResult.Id} deleted-status: " + delOptionResult?.StatusCode.ToString());

            var delResult = _bynderClient.GetAssetService().DeleteMetapropertyAsync(result.Id).GetAwaiter().GetResult();
            Logger.Log($"Metaproperty {result.Id} deleted-status: " + delResult?.StatusCode.ToString());
        }

        [TestMethod]
        public void TestFlow()
        {
            GetAccount();
            CreateAssetUsage();
            GetAssetByAssetId();
            PostAssetMetaproperties();
            GetAssetCollection();
            DeleteAssetUsage();
        }

        [TestMethod/*, Ignore("only manual")*/]
        public async Task UpdateMetapropertyOption()
        {
            // First create the metaproperty and option
            PostMetapropertyOptions(true);

            Assert.IsNotNull(createdObjects);
            Assert.IsNotEmpty(createdObjects);
            Assert.IsTrue(createdObjects.ContainsKey("mp"));
            Assert.IsNotNull(createdObjects["mp"]);
            Assert.IsTrue(createdObjects.ContainsKey("option"));
            Assert.IsNotNull(createdObjects["option"]);

            /**
             * In plain terms: the ID exists, but the backend system (Bynder, in this case) hasn’t finished making that object queryable when you ask for it.
             * Sometimes you hit the window where it’s ready, sometimes you don't.
             */
            var options = await GetOptionsWithRetryAsync(new List<string> { createdObjects["option"] });

            Assert.IsNotNull(options);
            Assert.IsNotEmpty(options);

            var option = options.FirstOrDefault();
            Assert.IsNotNull(option);

            option.ZIndex = "18";

            var optionUpdateResult = _bynderClient.GetAssetService().UpsertMetapropertyOptionAsync(createdObjects["mp"], option).GetAwaiter().GetResult();
            Assert.IsNotNull(optionUpdateResult);

            Logger.Log($"Option {createdObjects["option"]} update result: {optionUpdateResult?.StatusCode}");
            Assert.AreEqual(201, optionUpdateResult.StatusCode);

            DeleteMetapropertyOption(createdObjects["mp"], createdObjects["option"]);
            DeleteMetaproperty(createdObjects["mp"]);
        }

        [DataRow(123)]
        [Ignore("Add valid entity id here")]
        [TestMethod]
        public void UploadEntityTest(int entityId)
        {
            Uploader uploader = new Uploader() { Context = InRiverContext };
            uploader.Context.Settings = TestSettings;
            uploader.EntityUpdated(entityId, null);
        }

        [TestMethod]
        public void UpsertMetaproperty()
        {
            var guid = Guid.NewGuid();
            var metaproperty = new Bynder.Sdk.Model.Metaproperty()
            {
                Type = MetapropertyType.Text,
                IsRequired = false,
                IsFilterable = false,
                Label = "API TEST metaproperty " + guid,
                Name = "ApiTestMetaProperty" + guid,
                ZIndex = 100,
                IsEditable = false,
                IsMainfilter = false,
                IsMultiSelect = false,
            };

            var result = _bynderClient.GetAssetService().UpsertMetapropertyAsync(metaproperty).GetAwaiter().GetResult();
            Assert.IsNotNull(result);
            Assert.AreNotEqual(string.Empty, result.Id);
            Assert.AreNotEqual("00000000-0000-0000-0000000000000000", result.Id);
            Assert.AreEqual(201, result.StatusCode);

            var delResult = _bynderClient.GetAssetService().DeleteMetapropertyAsync(result.Id).GetAwaiter().GetResult();
            Console.WriteLine(delResult?.StatusCode);
        }

        private async Task<IEnumerable<Bynder.Sdk.Model.MetapropertyOption>> GetOptionsWithRetryAsync(
            IList<string> ids,
            int maxAttempts = 5,
            int delayMs = 500)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var options = await _bynderClient
                    .GetAssetService()
                    .GetMetapropertyOptionsByIdAsync(ids);

                if (options != null && options.Any())
                    return options;

                await Task.Delay(delayMs);
            }

            return new List<Bynder.Sdk.Model.MetapropertyOption>();
        }

        #endregion Methods
    }
}