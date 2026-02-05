using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetUploadWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.BynderLocaleForMetapropertyOptionLabel, string.Empty },
                { Settings.BynderBrandName, string.Empty },
            };

            return settings;
        }
    }
}
