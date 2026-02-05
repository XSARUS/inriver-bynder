using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetUsageUpdateWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {

            var settings = new Dictionary<string, string>()
            {
                { Settings.InRiverIntegrationId, @"41a92562-bfd9-4847-a34d-4320bcef5e4a" },
                { Settings.InRiverEntityUrl, @"https://inriver.productmarketingcloud.com/app/enrich#entity/{entityId}/" },
            };

            return settings;
        }
    }
}
