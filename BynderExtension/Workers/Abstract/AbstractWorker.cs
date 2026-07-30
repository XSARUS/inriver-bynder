using inRiver.Remoting.Extension;
using System.Collections.Generic;

namespace Bynder.Workers
{
    public class AbstractWorker
    {
        #region Properties

        public virtual Dictionary<string, string> DefaultSettings { get; set; }
        public inRiverContext InRiverContext { get; set; }

        #endregion Properties

        #region Constructors

        public AbstractWorker(inRiverContext inRiverContext)
        {
            InRiverContext = inRiverContext;
        }

        #endregion Constructors
    }
}