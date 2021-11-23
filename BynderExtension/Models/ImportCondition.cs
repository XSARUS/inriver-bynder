using Bynder.Enums;
using System.Collections.Generic;

namespace Bynder.Models
{
    public class ImportCondition
    {
        public string PropertyName { get; set; }
        /// <summary>
        /// Values from bynder are always represented as array, so we will define them as such in the conditions as well
        /// </summary>
        public List<string> Values { get; set; } = new List<string>();
        public MatchType MatchType { get; set; } = MatchType.EqualSorted;
    }
}
