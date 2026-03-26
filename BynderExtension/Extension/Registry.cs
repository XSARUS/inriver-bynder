using inRiver.Remoting.Extension;
using System;

namespace Bynder.Extension
{
    using Api;
    using Sdk.Settings;
    using Utils;
    using Utils.Traverser;
    using Workers;
    using SdkBynderClient = Sdk.Service.BynderClient;
    using SdkIBynderClient = Sdk.Service.IBynderClient;

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
            For<Configuration>().Use(new Configuration
            {
                BaseUrl = new Uri(inRiverContext.Settings[SettingNames.BynderClientUrl]),
                ClientId = inRiverContext.Settings[SettingNames.BynderClientId],
                ClientSecret = inRiverContext.Settings[SettingNames.BynderSecretId]
            });

            // file name evaluator
            For<FilenameEvaluator>().Use<FilenameEvaluator>();

            // traversers
            For<EntityTraverser>().Use<EntityTraverser>();
            For<MetapropertyMapTraverser>().Use<MetapropertyMapTraverser>();

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