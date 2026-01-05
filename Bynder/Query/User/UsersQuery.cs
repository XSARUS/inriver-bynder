using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Decoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Query.User
{
    public class UsersQuery
    {
        [ApiField("includeInActive")]
        public string IncludeInActive { get; set; } = false.ToString();

        /// <summary>
        /// Maximum number of results.
        /// </summary>
        [ApiField("limit")]
        public int Limit { get; set; } = 100;

        /// <summary>
        /// Offset page for results: return the N-th set of limit-results.
        /// </summary>
        [ApiField("page")]
        public int Page { get; set; } = 1;
    }
}
