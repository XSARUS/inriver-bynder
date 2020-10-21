using System.Collections.Generic;
using Bynder.Api;
using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BynderTest
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; }
        protected inRiverContext InRiverContext;
        protected Logger Logger;

        // todo: Add your bynder connection settings here
        protected readonly BynderClientSettings _bynderSettings = new BynderClientSettings()
        {
            ConsumerKey = "***",
            ConsumerSecret = "***",
            CustomerBynderUrl = "***",
            Token = "***",
            TokenSecret = "***"
        };

        // todo: Add your settings here
        protected Dictionary<string,string> TestSettings = new Dictionary<string, string>
        {
            {Bynder.Api.SettingNames.ConsumerKey, "***" },
            {Bynder.Api.SettingNames.ConsumerSecret, "***" },
            {Bynder.Api.SettingNames.CustomerBynderUrl, "***.getbynder.com" },
            {Bynder.Api.SettingNames.Token, "***" },
            {Bynder.Api.SettingNames.TokenSecret, "***" },
            {Bynder.Config.Settings.RegularExpressionForFileName, @"^(?<ProductNumber>[0-9a-zA-Z]+)_(?<ResourceType>image|document)_(?<ResourcePosition>[0-9]+)" },
            {Bynder.Config.Settings.InitialAssetLoadUrlQuery, @"type=image" },
            {Bynder.Config.Settings.MetapropertyMap, @"C7BC01E1-670D-4410-A7B81E9032FE261A=ResourcePosition,C284234B-29B6-4CA8-B907B728455F30EA=ProductNumber" },
            {Bynder.Config.Settings.bynderBrandName, "" }
        };

        [TestInitialize]
        public void TestInitialize()
        {
            Logger = new Logger(TestContext);
            Logger.Log(LogLevel.Information, $"Initialize connection to inRiver Server");

            // Customer: https://remoting.productmarketingcloud.com
            // Sandbox:  https://partner.remoting.productmarketingcloud.com

            // todo: add your inRiver username, password and environment here 
            InRiverContext = new inRiverContext(
                RemoteManager.CreateInstance("https://partner.remoting.productmarketingcloud.com",
                    "***", "***", "***"), Logger);

            Assert.IsNotNull(InRiverContext?.ExtensionManager, "Connection to inRiver failed. Please check the url and credentials within the test initialize method.");
        }
    }
}
