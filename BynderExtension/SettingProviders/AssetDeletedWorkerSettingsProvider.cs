using System.Collections.Generic;

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
