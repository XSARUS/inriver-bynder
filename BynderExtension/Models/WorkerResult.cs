using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Models
{
    /// <summary>
    /// basic worker result
    /// </summary>
    public class WorkerResult
    {
        #region Properties

        public List<string> Messages { get; set; } = new List<string>();

        #endregion Properties
    }
}