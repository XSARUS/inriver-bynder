using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class NonResourceMetapropertyWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                 { Settings.MetapropertyMap, "" },
            };

            return settings;
        }

        #endregion Methods
    }
}