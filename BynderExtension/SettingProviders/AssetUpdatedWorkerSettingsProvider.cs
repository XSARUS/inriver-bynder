using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;
    using Enums;

    public static class AssetUpdatedWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.ResourceSearchType, ResourceSearchType.AssetId.ToString() },
                { Settings.ImportConditions, "[{\"propertyName\":\"SyncToInriver\",\"values\":[\"True\"], \"matchType\":\"Equal\"}]" },
                { Settings.FilenameExtensionMediaTypeMapping, string.Empty },
                { Settings.AddAssetIdPrefixToFilenameOfNewResource, true.ToString() },
                { Settings.DownloadMediaType, "original" },
                { Settings.CreateMissingCvlKeys, true.ToString() },
                { Settings.MultivalueSeparator, "," },
                { Settings.LocaleStringLanguagesToSet, string.Empty },
                { Settings.AssetPropertyMap, "description=ResourceDescription" },
                { Settings.MetapropertyMap, "metapropertyguid1=inriverfield1,metapropertyguid2=inriverfield2" },
                { Settings.FieldValuesToSetOnArchiveEvent, string.Empty },
                { Settings.TimestampSettings, string.Empty },
                { Settings.FieldTypeThumbnailMapping, string.Empty  }
            };

            foreach (var setting in FilenameEvaluatorSettingsProvider.Create())
            {
                settings[setting.Key] = setting.Value;
            }

            return settings;
        }

        #endregion Methods
    }
}