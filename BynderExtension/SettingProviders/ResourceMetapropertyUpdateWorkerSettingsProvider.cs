using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class ResourceMetapropertyUpdateWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.MetapropertyMap, "metapropertyguid1=inriverfield1,metapropertyguid2=inriverfield2" },
                { Settings.ExportConditions, "[{\"inRiverFieldTypeId\":\"ResourceSyncToBynder\",\"values\":[\"True\"], \"matchType\":\"Equal\"}]" },
            };

            return settings;
        }
    }
}
