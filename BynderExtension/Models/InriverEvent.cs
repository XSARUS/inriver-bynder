namespace Bynder.Models
{
    public class InriverEvent
    {
        public int EntityId { get; set; }
        public bool IsResource { get; set; }
        public bool IsLink { get; set; }

        /// <summary>
        /// Used for retrying failed events. Incremented each time the event fails to be processed.
        /// </summary>
        public int Attempt { get; set; }
        public string[] FieldTypeIds { get; set; }
    }
}
