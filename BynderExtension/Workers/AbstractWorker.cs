using inRiver.Remoting.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Workers
{
    public class AbstractWorker
    {
        #region Properties

        public virtual Dictionary<string, string> DefaultSettings { get; set; }
        public inRiverContext InRiverContext { get; set; }

        #endregion Properties

        public AbstractWorker(inRiverContext inRiverContext)
        {
            InRiverContext = inRiverContext;
        }
    }
}