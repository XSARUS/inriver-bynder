using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetDownloadWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.FilenameExtensionMediaTypeMapping, string.Empty },
                { Settings.DownloadMediaType, "original" },
            };

            return settings;
        }
    }
}
