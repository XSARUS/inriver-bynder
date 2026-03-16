using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class ResourceMetapropertyUpdateWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.MetapropertyMapToBynder, "" },
                { Settings.ExportConditions, "[{\"inRiverFieldTypeId\":\"ResourceSyncToBynder\",\"values\":[\"True\"], \"matchType\":\"Equal\"}]" },
            };

            return settings;
        }

        #endregion Methods
    }
}