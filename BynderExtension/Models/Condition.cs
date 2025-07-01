using System.Collections.Generic;

namespace Bynder.Models
{
    using Enums;

    public abstract class Condition
    {
        public MatchType MatchType { get; set; } = MatchType.EqualSorted;

        /// <summary>
        /// Values from bynder are always represented as array, so we will define them as such in the conditions as well
        /// </summary>
        public List<string> Values { get; set; } = new List<string>();
    }
}
