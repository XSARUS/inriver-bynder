using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetUsageUpdateWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.InRiverIntegrationId, @"41a92562-bfd9-4847-a34d-4320bcef5e4a" },
                { Settings.InRiverEntityUrl, @"https://inriver.productmarketingcloud.com/app/enrich#entity/{entityId}/" },
            };

            return settings;
        }

        #endregion Methods
    }
}