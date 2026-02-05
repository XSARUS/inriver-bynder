using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;
    using Enums;

    public static class AssetUpdatedWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {

            var settings = new Dictionary<string, string>()
            {
                { Settings.ResourceSearchType, ResourceSearchType.AssetId.ToString() },
                { Settings.ImportConditions, "[{\"propertyName\":\"SyncToInriver\",\"values\":[\"True\"], \"matchType\":\"Equal\"}]" },
                { Settings.AddAssetIdPrefixToFilenameOfNewResource, true.ToString() },
                { Settings.CreateMissingCvlKeys, true.ToString() },
                { Settings.MultivalueSeparator, "," },
                { Settings.LocaleStringLanguagesToSet, string.Empty },
                { Settings.AssetPropertyMap, "description=ResourceDescription" },
                { Settings.MetapropertyMap, "metapropertyguid1=inriverfield1,metapropertyguid2=inriverfield2" },
                { Settings.FieldValuesToSetOnArchiveEvent, string.Empty },
                { Settings.TimestampSettings, string.Empty },
            };

            foreach (var setting in FilenameEvaluatorSettingsProvider.Create())
            {
                settings[setting.Key] = setting.Value;
            }

            return settings;
        }
    }
}
