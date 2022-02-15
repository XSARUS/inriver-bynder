using System.Collections.Generic;

namespace Bynder.Workers
{
    public interface IWorker
    {
    }

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