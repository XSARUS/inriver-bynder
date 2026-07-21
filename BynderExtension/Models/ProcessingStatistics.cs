namespace Bynder.Models
{
    public class ProcessingStatistics
    {
        public int Retried { get; set; }
        public int Failed { get; set; }
        public int Successful { get; set; }
        public int Deleted { get; set; }
    }
}
