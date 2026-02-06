using inRiver.Remoting.Extension;

namespace Bynder.Workers
{
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    public class AbstractBynderWorker: AbstractWorker
    {
        #region Properties
        protected readonly SdkIBynderClient _bynderClient;
        #endregion Properties

        public AbstractBynderWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null)
            : base(inRiverContext)
        {
            _bynderClient = bynderClient;
        }
    }
}