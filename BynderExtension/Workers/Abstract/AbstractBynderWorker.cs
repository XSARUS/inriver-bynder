using inRiver.Remoting.Extension;

namespace Bynder.Workers
{
    using SdkIBynderClient = Sdk.Service.IBynderClient;

    public class AbstractBynderWorker : AbstractWorker
    {
        #region Fields

        protected readonly SdkIBynderClient _bynderClient;

        #endregion Fields

        #region Constructors

        public AbstractBynderWorker(inRiverContext inRiverContext, SdkIBynderClient bynderClient = null)
            : base(inRiverContext)
        {
            _bynderClient = bynderClient;
        }

        #endregion Constructors
    }
}