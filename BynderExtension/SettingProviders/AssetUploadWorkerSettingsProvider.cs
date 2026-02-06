using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetUploadWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.BynderLocaleForMetapropertyOptionLabel, string.Empty },
                { Settings.BynderBrandName, string.Empty },
            };

            return settings;
        }

        #endregion Methods
    }
}