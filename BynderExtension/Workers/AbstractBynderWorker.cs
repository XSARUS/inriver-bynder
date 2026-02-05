using inRiver.Remoting.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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