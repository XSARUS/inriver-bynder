using System.Collections.Generic;

namespace Bynder.Api
{
    public static class SettingNames
    {
        #region Fields

        public const string BynderClientId = "BYNDER_CLIENT_ID";
        public const string BynderClientUrl = "BYNDER_CLIENT_URL";
        public const string BynderSecretId = "BYNDER_CLIENT_SECRET";

        #endregion Fields

        #region Methods

        /// <summary>
        /// One should configure an oauth2 app of type "credentials" within Bynder
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetDefaultBynderApiSettings()
            => new Dictionary<string, string>
            {
                {BynderClientUrl, "https://[CLIENT].getbynder.com"},
                {BynderClientId, "<YOUR BYNDER CLIENT_ID>"}, // CLIENT_ID OF BYNDER OAUTH_APP
                {BynderSecretId, "<YOUR BYNDER CLIENT_SECRET>"},  // CLIENT_SECRET OF BYNDER OAUTH_APP
            };

        #endregion Methods
    }
}