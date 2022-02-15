using System;
using System.Collections.Generic;

namespace Bynder.Api
{
    public class BynderClientSettings
    {
        #region Properties

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string CustomerBynderUrl { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }

        #endregion Properties

        #region Methods

        public static BynderClientSettings Create(Dictionary<string, string> connectorSettings)
        {
            try
            {
                var clientsettings = new BynderClientSettings()
                {
                    ConsumerKey = connectorSettings[SettingNames.ConsumerKey],
                    ConsumerSecret = connectorSettings[SettingNames.ConsumerSecret],
                    CustomerBynderUrl = connectorSettings[SettingNames.CustomerBynderUrl],
                    Token = connectorSettings[SettingNames.Token],
                    TokenSecret = connectorSettings[SettingNames.TokenSecret],
                };
                return clientsettings;
            }
            catch (KeyNotFoundException e)
            {
                throw new ArgumentException("Cannot create clientsettings, API-settings are missing in the dictionary",
                    e.InnerException);
            }
        }

        #endregion Methods
    }
}