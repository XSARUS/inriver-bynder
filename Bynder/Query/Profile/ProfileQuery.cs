using Bynder.Sdk.Query.Decoder;

namespace Bynder.Query.Profile
{
    public class ProfileQuery
    {
        #region Properties

        [ApiField("id")]
        public string Id { get; set; }

        #endregion Properties
    }
}