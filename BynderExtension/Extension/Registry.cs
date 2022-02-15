using Bynder.Api;
using Bynder.Utils;
using Bynder.Workers;
using inRiver.Remoting.Extension;

namespace Bynder.Extension
{
    internal class Registry : StructureMap.Registry
    {
        #region Constructors

        public Registry(inRiverContext inRiverContext)
        {
            // inRiver Context
            For<inRiverContext>().Use(inRiverContext);

            // Bynder API Client
            For<IBynderClient>().Use<BynderClient>();

            // Bynder API Client configuration
            For<BynderClientSettings>().Use(BynderClientSettings.Create(inRiverContext.Settings));

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