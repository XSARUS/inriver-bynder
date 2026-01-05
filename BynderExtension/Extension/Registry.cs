using inRiver.Remoting.Extension;
using System;

namespace Bynder.Extension
{
    using Api;
    using Bynder.Sdk.Settings;
    using Utils;
    using Workers;
    using SdkBynderClient = Bynder.Sdk.Service.BynderClient;
    using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;

    internal class Registry : StructureMap.Registry
    {
        #region Constructors

        public Registry(inRiverContext inRiverContext)
        {
            // inRiver Context
            For<inRiverContext>().Use(inRiverContext);

            // Bynder API Client
            For<SdkIBynderClient>().Use<SdkBynderClient>();

            // Bynder API Client configuration
            For<BynderClientSettings>().Use(BynderClientSettings.Create(inRiverContext.Settings));

            // Bynder API Client configuration
            For<Configuration>().Use(new Configuration { 
                BaseUrl = new Uri(inRiverContext.Settings[SettingNames.CustomerBynderUrl]),
                ClientId = inRiverContext.Settings[SettingNames.ConsumerKey],
                ClientSecret = inRiverContext.Settings[SettingNames.ConsumerSecret]
            });

            // file name evaluator
            For<FilenameEvaluator>().Use<FilenameEvaluator>();

            // auto add the workers
            Scan(x =>
            {
                x.TheCallingAssembly();
                x.AddAllTypesOf<IWorker>().NameBy(type => type.Name);
            }
            );
        }

        #endregion Constructors
    }
}