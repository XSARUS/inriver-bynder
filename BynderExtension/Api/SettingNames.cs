﻿using System.Collections.Generic;

namespace Bynder.Api
{
    public static class SettingNames
    {
        #region Fields

        public const string ConsumerKey = "CONSUMER_KEY";
        public const string ConsumerSecret = "CONSUMER_SECRET";
        public const string CustomerBynderUrl = "CUSTOMER_BYNDER_URL";
        public const string Token = "TOKEN";
        public const string TokenSecret = "TOKEN_SECRET";

        #endregion Fields

        #region Methods

        public static Dictionary<string, string> GetDefaultBynderApiSettings()
            => new Dictionary<string, string>
            {
                {CustomerBynderUrl, "https://[CUSTOMER].getbynder.com"},
                {ConsumerKey, "YOUR-CONSUMER-KEY"},
                {ConsumerSecret, "YOUR-CONSUMER-SECRET"},
                {Token, "YOUR-TOKEN"},
                {TokenSecret, "YOUR-TOKEN-SECRET"}
            };

        #endregion Methods
    }
}