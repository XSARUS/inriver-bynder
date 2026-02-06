using Bynder.Sdk.Query.Decoder;

namespace Bynder.Query.User
{
    public class UsersQuery
    {
        #region Properties

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

        #endregion Properties
    }
}