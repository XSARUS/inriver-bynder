using System.Collections.Generic;

namespace Bynder.Models
{
    public class ImportCondition
    {
        public string PropertyName { get; set; }
        /// <summary>
        /// Values from bynder are always represented as array 
        /// </summary>
        public List<string> Values { get; set; } = new List<string>();
    }
}
