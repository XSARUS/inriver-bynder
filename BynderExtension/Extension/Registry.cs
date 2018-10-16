using Bynder.Api;
using Bynder.Utils;
using Bynder.Workers;
using inRiver.Remoting.Extension;

namespace Bynder.Extension
{
    class Registry : StructureMap.Registry
    {
        public Registry(inRiverContext inRiverContext)
        {
            // inRiver Context
            For<inRiverContext>().Use(inRiverContext);

            // Bynder API Client
            For<IBynderClient>().Use<BynderClient>();
            
            // Bynder API Client configuration
            For<BynderClientSettings>().Use(BynderClientSettings.Create(inRiverContext.Settings));

            // file name evaluator
            For<FileNameEvaluator>().Use<FileNameEvaluator>();

            // auto add the workers
            Scan(x =>
                {
                    x.TheCallingAssembly();
                    x.AddAllTypesOf<IWorker>().NameBy(type => type.Name);
                }
            );
        }
    }
}
