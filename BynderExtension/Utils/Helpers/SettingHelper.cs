using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Utils.Helpers
{
    using Config;
    using Enums;
    using Models;
    using Utils.Extensions;

    public static class SettingHelper
    {
        #region Methods

        public static string GetBynderBrandName(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.BynderBrandName))
            {
                return settings[Settings.BynderBrandName];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.BynderBrandName}'");

            return string.Empty;
        }

        public static string GetCronExpression(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.CronExpression))
            {
                return settings[Settings.CronExpression];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.CronExpression}'. Using default value '* * * * *'");

            return "* * * * *";
        }

        /// <summary>
        /// Optional setting. Default is an empty dictionary
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetConfiguredAssetPropertyMap(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.AssetPropertyMap))
            {
                return settings[Settings.AssetPropertyMap].ToDictionary<string, string>(',', '=');
            }
            logger.Log(LogLevel.Verbose, "Could not find configured asset property Map");
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Optional setting. Default is false.
        /// </summary>
        /// <returns></returns>
        public static bool GetConfiguredCreateMissingCvlKeys(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.CreateMissingCvlKeys))
            {
                return string.Equals(settings[Settings.CreateMissingCvlKeys], true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.CreateMissingCvlKeys}'. Using default value '{false}'");

            return false;
        }

        /// <summary>
        /// Optional setting. Default is an empty dictionary.
        /// </summary>
        /// <returns></returns>
        public static List<MetaPropertyMap> GetConfiguredMetaPropertyMap(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.MetapropertyMap))
            {
                var settingValue = settings[Settings.MetapropertyMap];
                if (string.IsNullOrEmpty(settingValue))
                {
                    return new List<MetaPropertyMap>();
                }

                settingValue = settingValue.Trim();
                if (settingValue.StartsWith("[") && settingValue.EndsWith("]"))
                {
                    return JsonConvert.DeserializeObject<List<MetaPropertyMap>>(settingValue)
                        .Where(map => !string.IsNullOrEmpty(map.BynderMetaProperty) && !string.IsNullOrEmpty(map.InriverFieldTypeId))
                        .ToList();
                }

                // support old format for backwards compatiblity
                var mapDict = settings[Settings.MetapropertyMap].ToDictionary<string, string>(',', '=');
                return mapDict
                    .Select(x => new MetaPropertyMap { BynderMetaProperty = x.Key, InriverFieldTypeId = x.Value, IsMultiValue = true })
                    .Where(map => !string.IsNullOrEmpty(map.BynderMetaProperty) && !string.IsNullOrEmpty(map.InriverFieldTypeId))
                    .ToList();
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.MetapropertyMap}'");

            return new List<MetaPropertyMap>();
        }

        public static DateTimeSettings GetDateTimeSettings(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.TimestampSettings))
            {
                return JsonConvert.DeserializeObject<DateTimeSettings>(settings[Settings.TimestampSettings]);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.TimestampSettings}");
            return null;
        }

        /// <summary>
        /// Optional setting. Default is false.
        /// </summary>
        /// <returns></returns>
        public static bool GetDeleteResourceOnDeletedEvents(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.DeleteResourceOnDeleteEvent))
            {
                return string.Equals(settings[Settings.DeleteResourceOnDeleteEvent], true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.DeleteResourceOnDeleteEvent}'. Using default value '{false}'");

            return false;
        }

        public static string GetDownloadMediaType(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.DownloadMediaType))
            {
                return settings[Settings.DownloadMediaType];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.DownloadMediaType}'. Using default value 'original'");

            return "original";
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static List<ExportCondition> GetExportConditions(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.ExportConditions))
            {
                return JsonConvert.DeserializeObject<List<ExportCondition>>(settings[Settings.ExportConditions]);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.ExportConditions}");
            return new List<ExportCondition>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static List<FieldValueCombination> GetFieldValueCombinations(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.FieldValuesToSetOnArchiveEvent))
            {
                return JsonConvert.DeserializeObject<List<FieldValueCombination>>(settings[Settings.FieldValuesToSetOnArchiveEvent]);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.FieldValuesToSetOnArchiveEvent}");
            return new List<FieldValueCombination>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static List<ImportCondition> GetImportConditions(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.ImportConditions))
            {
                return JsonConvert.DeserializeObject<List<ImportCondition>>(settings[Settings.ImportConditions]);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configured {Settings.ImportConditions}");
            return new List<ImportCondition>();
        }

        public static string GetInitialAssetLoadUrlQuery(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.InitialAssetLoadUrlQuery))
            {
                return settings[Settings.InitialAssetLoadUrlQuery];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.InitialAssetLoadUrlQuery}'");

            return string.Empty;
        }

        public static string GetInRiverEntityUrl(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.InRiverEntityUrl))
            {
                return settings[Settings.InRiverEntityUrl];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.InRiverEntityUrl}'");

            return string.Empty;
        }

        public static string GetInRiverIntegrationId(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.InRiverIntegrationId))
            {
                return settings[Settings.InRiverIntegrationId];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.InRiverIntegrationId}'");

            return string.Empty;
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetLanguagesToSet(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.LocaleStringLanguagesToSet))
            {
                return settings[Settings.LocaleStringLanguagesToSet].ConvertTo<IEnumerable<string>>() ?? new List<string>();
            }

            logger.Log(LogLevel.Verbose, "Could not find configured multivalue separator");

            return new List<string>();
        }

        /// <summary>
        /// Optional setting. Default is an empty string.
        /// </summary>
        /// <returns></returns>
        public static string GetMultivalueSeparator(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.MultivalueSeparator))
            {
                return settings[Settings.MultivalueSeparator];
            }

            logger.Log(LogLevel.Verbose, "Could not find configured multivalue separator");
            return string.Empty;
        }

        public static string GetRegularExpressionForFileName(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.RegularExpressionForFileName))
            {
                return settings[Settings.RegularExpressionForFileName];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.RegularExpressionForFileName}'");

            return string.Empty;
        }

        public static ResourceSearchType GetResourceSearchType(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.TryGetValue(Settings.ResourceSearchType, out string setting))
            {
                return setting.ToEnum<ResourceSearchType>();
            }
            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.ResourceSearchType}'. Using default value '{ResourceSearchType.AssetId.ToString()}'");

            // default ResourceSearchType.AssetId for backwards compatiblity
            return ResourceSearchType.AssetId;
        }

        public static bool ShouldAddAssetIdPrefixToFilename(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.TryGetValue(Settings.AddAssetIdPrefixToFilenameOfNewResource, out string setting))
            {
                return string.Equals(setting, true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.AddAssetIdPrefixToFilenameOfNewResource}'. Using default value '{true}'");

            // default true for backwards compatiblity
            return true;
        }

        #endregion Methods
    }
}