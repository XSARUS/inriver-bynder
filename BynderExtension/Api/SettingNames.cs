using System.Collections.Generic;

namespace Bynder.Api
{
    public static class SettingNames
    {
        #region Fields

        public const string ConsumerKey = "CONSUMER_KEY";
        public const string ConsumerSecret = "CONSUMER_SECRET";
        public const string CustomerBynderUrl = "CUSTOMER_BYNDER_URL";

        #endregion Fields

        #region Methods

        /// <summary>
        /// One should configure an oauth2 app of type "credentials" within Bynder
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetDefaultBynderApiSettings()
            => new Dictionary<string, string>
            {
                {CustomerBynderUrl, "https://[CUSTOMER].getbynder.com"},
                {ConsumerKey, "YOUR-CONSUMER-KEY"}, // CLIENT_ID OF BYNDER OAUTH_APP
                {ConsumerSecret, "YOUR-CONSUMER-SECRET"},  // CLIENT_SECRET OF BYNDER OAUTH_APP
            };

        #endregion Methods
    }
}