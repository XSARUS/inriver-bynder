using System.Collections.Generic;

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