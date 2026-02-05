using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    public static class AssetDeletedWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            return new Dictionary<string, string>()
            {
                { Config.Settings.DeleteResourceOnDeleteEvent, false.ToString() },
            };
        }
    }
}
