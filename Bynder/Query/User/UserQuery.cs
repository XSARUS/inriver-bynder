using Bynder.Sdk.Query.Decoder;

namespace Bynder.Query.User
{
    public class UserQuery
    {
        #region Properties

        [ApiField("id")]
        public string Id { get; set; }

        #endregion Properties
    }
}