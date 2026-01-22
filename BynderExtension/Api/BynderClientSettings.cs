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

        #endregion Properties

        #region Methods

        public static BynderClientSettings Create(Dictionary<string, string> connectorSettings)
        {
            try
            {
                var clientsettings = new BynderClientSettings()
                {
                    ConsumerKey = connectorSettings[SettingNames.BynderClientId],
                    ConsumerSecret = connectorSettings[SettingNames.BynderSecretId],
                    CustomerBynderUrl = connectorSettings[SettingNames.BynderClientUrl]
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