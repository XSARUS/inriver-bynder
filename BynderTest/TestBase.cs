using Bynder.Api;
using Bynder.Enums;
using Bynder.Sdk.Settings;
using BynderClient = Bynder.Sdk.Service.BynderClient;
using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BynderTest
{
    /// <summary>
    /// Fill this class with your extension settings and inriver remoting connection details
    /// </summary>
    [TestClass]
    public abstract class TestBase
    {
        #region Fields

        protected inRiverContext InRiverContext;
        protected Logger Logger;

        protected Dictionary<string, string> TestSettings = new Dictionary<string, string>
        {
            // set your settings here

            {SettingNames.BynderClientUrl, "https://[CUSTOMER].getbynder.com" },
            {SettingNames.BynderClientId, "***" },
            {SettingNames.BynderSecretId, "***" },

            {Bynder.Config.Settings.AddAssetIdPrefixToFilenameOfNewResource, false.ToString() },
            {Bynder.Config.Settings.AssetPropertyMap, "" },
            {Bynder.Config.Settings.BynderBrandName, "" },
            {Bynder.Config.Settings.CreateMissingCvlKeys, true.ToString() },
            {Bynder.Config.Settings.DeleteResourceOnDeleteEvent, true.ToString() },
            {Bynder.Config.Settings.DownloadMediaType, "webimage" },
            {Bynder.Config.Settings.FieldValuesToSetOnArchiveEvent, "[{\"fieldTypeId\":\"ResourceBynderArchived\",\"value\": true},{\"fieldTypeId\":\"ResourceBynderArchiveDate\",\"setTimestamp\": true}]" },
            {Bynder.Config.Settings.ImportConditions, "[{ \"propertyName\": \"synctoinriver\", \"values\": [\"true\"], \"matchType\":\"Equal\"}]" },
            {Bynder.Config.Settings.ExportConditions, "[{ \"inRiverFieldTypeId\":\"ResourceSyncToBynder\",\"values\":[\"True\"], \"matchType\":\"Equal\"}]" },
            {Bynder.Config.Settings.InitialAssetLoadUrlQuery, @"type=image,SyncToInriver=true" },
            {Bynder.Config.Settings.InRiverEntityUrl, "https://inriver.productmarketingcloud.com/app/enrich#entity/{entityId}/" },
            {Bynder.Config.Settings.InRiverIntegrationId, "" },
            {Bynder.Config.Settings.LocaleStringLanguagesToSet, "en-GB, nl-NL" },
            {Bynder.Config.Settings.MetapropertyMap, @"C7BC01E1-670D-4410-A7B81E9032FE261A=ResourcePosition,C284234B-29B6-4CA8-B907B728455F30EA=ProductNumber" },
            {Bynder.Config.Settings.MultivalueSeparator, ", " },
            {Bynder.Config.Settings.RegularExpressionForFileName, @"^(?<ProductNumber>[0-9a-zA-Z]+)_(?<ResourceType>image|document)_(?<ResourcePosition>[0-9]+)" },
            {Bynder.Config.Settings.ResourceSearchType, ResourceSearchType.AssetId.ToString() },
            {Bynder.Config.Settings.TimestampSettings, "{\"timestampType\": \"Utc\",\"localTimeZone\": \"W. Europe Standard Time\",\"localDstEnabled\": true}" },
        };

        private const string _customerRemotingUrl = "https://remoting.productmarketingcloud.com";
        // set your inriver user api key here
        private const string _customerRemotingApiKey = "***";

        #endregion Fields

        #region Properties

        public TestContext TestContext { get; set; }

        protected BynderClientSettings BynderSettings => new BynderClientSettings()
        {
            ConsumerKey = TestSettings[SettingNames.BynderClientId],
            ConsumerSecret = TestSettings[SettingNames.BynderSecretId],
            CustomerBynderUrl = TestSettings[SettingNames.BynderClientUrl],
        };

        internal BynderClient _bynderClient { get; set; }

        #endregion Properties

        #region Methods

        [TestInitialize]
        public void TestInitialize()
        {
            Logger = new Logger(TestContext);
            Logger.Log(LogLevel.Information, $"Initialize connection to inRiver Server");

            InRiverContext = new inRiverContext(RemoteManager.CreateInstance(_customerRemotingUrl, _customerRemotingApiKey), Logger);

            Assert.IsNotNull(InRiverContext?.ExtensionManager, "Connection to inRiver failed. Please check the url and credentials within the test initialize method.");
            InRiverContext.Settings = TestSettings;

            _bynderClient = new BynderClient(new Configuration()
            {
                BaseUrl = new System.Uri(BynderSettings.CustomerBynderUrl),
                ClientId = BynderSettings.ConsumerKey,
                ClientSecret = BynderSettings.ConsumerSecret,
            });
        }

        #endregion Methods
    }
}