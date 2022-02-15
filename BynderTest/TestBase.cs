using Bynder.Api;
using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BynderTest
{
    [TestClass]
    public class TestBase
    {
        #region Fields

        protected inRiverContext InRiverContext;
        protected Logger Logger;

        // todo: Add your settings here
        protected Dictionary<string, string> TestSettings = new Dictionary<string, string>
        {
            {Bynder.Api.SettingNames.CustomerBynderUrl, "***.getbynder.com" },
            {Bynder.Api.SettingNames.ConsumerKey, "***" },
            {Bynder.Api.SettingNames.ConsumerSecret, "***" },
            {Bynder.Api.SettingNames.Token, "***" },
            {Bynder.Api.SettingNames.TokenSecret, "***" },

            {Bynder.Config.Settings.InitialAssetLoadUrlQuery, @"type=image" },
            {Bynder.Config.Settings.RegularExpressionForFileName, @"^(?<ProductNumber>[0-9a-zA-Z]+)_(?<ResourceType>image|document)_(?<ResourcePosition>[0-9]+)" },
            {Bynder.Config.Settings.MetapropertyMap, @"C7BC01E1-670D-4410-A7B81E9032FE261A=ResourcePosition,C284234B-29B6-4CA8-B907B728455F30EA=ProductNumber" },
            {Bynder.Config.Settings.InRiverIntegrationId, "" },
            {Bynder.Config.Settings.InRiverEntityUrl, "https://inriver.productmarketingcloud.com/app/enrich#entity/{entityId}/" },
            {Bynder.Config.Settings.BynderBrandName, "" },
            {Bynder.Config.Settings.LocaleStringLanguagesToSet, "en-GB, nl-NL" },
            {Bynder.Config.Settings.MultivalueSeparator, ", " },
            {Bynder.Config.Settings.ImportConditions, "[\r\n  {\r\n    \"propertyName\": \"synctoinriver\",\r\n    \"values\": [\r\n      \"True\"\r\n    ]\r\n  }\r\n]" }
        };

        private const string _customerRemotingUrl = "https://remoting.productmarketingcloud.com";
        private const string _partnerRemotingUrl = "https://partner.remoting.productmarketingcloud.com";

        #endregion Fields

        #region Properties

        public TestContext TestContext { get; set; }

        protected BynderClientSettings BynderSettings => new BynderClientSettings()
        {
            ConsumerKey = TestSettings[SettingNames.ConsumerKey],
            ConsumerSecret = TestSettings[SettingNames.ConsumerSecret],
            CustomerBynderUrl = TestSettings[SettingNames.CustomerBynderUrl],
            Token = TestSettings[SettingNames.Token],
            TokenSecret = TestSettings[SettingNames.TokenSecret]
        };

        #endregion Properties

        #region Methods

        [TestInitialize]
        public void TestInitialize()
        {
            Logger = new Logger(TestContext);
            Logger.Log(LogLevel.Information, $"Initialize connection to inRiver Server");

            // todo: add your inRiver username, password and environment here
            InRiverContext = new inRiverContext(
                RemoteManager.CreateInstance(_partnerRemotingUrl,
                    "***", "***", "***"), Logger);

            Assert.IsNotNull(InRiverContext?.ExtensionManager, "Connection to inRiver failed. Please check the url and credentials within the test initialize method.");
        }

        #endregion Methods
    }
}