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
    using Extensions;
    using Models;

    public static class SettingHelper
    {
        #region Methods

        public static bool ExecuteBaseTestMethod(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.ExecuteBaseTestMethod) && settings.TryGetValue(Settings.ExecuteBaseTestMethod, out string setting))
            {
                return string.Equals(setting, true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.ExecuteBaseTestMethod}'. Using default value '{true}'");

            // default true for backwards compatiblity
            return true;
        }

        public static string GetBynderBrandName(Dictionary<string, string> settings, IExtensionLog logger)
        {
            string brandNameBySetting = string.Empty;

            if (settings.ContainsKey(Settings.BynderBrandName))
            {
                brandNameBySetting = settings[Settings.BynderBrandName];
                if (string.IsNullOrWhiteSpace(brandNameBySetting?.Trim()))
                {
                    logger.Log(LogLevel.Verbose, $"Configuration for '{Settings.BynderBrandName}' is empty!");
                }
            }
            else
            {
                logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.BynderBrandName}'");
            }

            return brandNameBySetting;
        }

        public static string GetBynderLocaleForMetapropertyOptionLabel(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.BynderLocaleForMetapropertyOptionLabel))
            {
                return settings[Settings.BynderLocaleForMetapropertyOptionLabel];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.BynderLocaleForMetapropertyOptionLabel}'");
            return string.Empty;
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

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.AssetPropertyMap}'");
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

        public static string GetCronExpression(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.CronExpression))
            {
                return settings[Settings.CronExpression];
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.CronExpression}'. Using default value '* * * * *'");

            return "* * * * *";
        }

        public static Dictionary<string, List<string>> GetCvlMetapropertyMapping(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.CvlMetapropertyMapping) && !string.IsNullOrWhiteSpace(settings[Settings.CvlMetapropertyMapping]))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(settings[Settings.CvlMetapropertyMapping]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.CvlMetapropertyMapping}'");
            return new Dictionary<string, List<string>>();
        }

        public static DateTimeSettings GetDateTimeSettings(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.TimestampSettings) && !string.IsNullOrWhiteSpace(settings[Settings.TimestampSettings]))
            {
                return JsonConvert.DeserializeObject<DateTimeSettings>(settings[Settings.TimestampSettings]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.TimestampSettings}'");
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
            if (settings.ContainsKey(Settings.ExportConditions) && !string.IsNullOrWhiteSpace(settings[Settings.ExportConditions]))
            {
                return JsonConvert.DeserializeObject<List<ExportCondition>>(settings[Settings.ExportConditions]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.ExportConditions}'");
            return new List<ExportCondition>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static List<FieldValueCombination> GetFieldValueCombinations(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.FieldValuesToSetOnArchiveEvent) && !string.IsNullOrWhiteSpace(settings[Settings.FieldValuesToSetOnArchiveEvent]))
            {
                return JsonConvert.DeserializeObject<List<FieldValueCombination>>(settings[Settings.FieldValuesToSetOnArchiveEvent]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.FieldValuesToSetOnArchiveEvent}'");
            return new List<FieldValueCombination>();
        }

        public static Dictionary<string, List<MediaTypeTransformConfig>> GetFilenameExtensionMediaTypeMapping(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.FilenameExtensionMediaTypeMapping) && !string.IsNullOrWhiteSpace(settings[Settings.FilenameExtensionMediaTypeMapping]))
            {
                var femTypeMappingSetting = settings[Settings.FilenameExtensionMediaTypeMapping];
                logger.Log(LogLevel.Debug, $"RAW JSON from setting 'Settings.FilenameExtensionMediaTypeMapping': '{femTypeMappingSetting}'");

                if (femTypeMappingSetting != "[]" && femTypeMappingSetting != "{}" && femTypeMappingSetting != "[{}]")
                {
                    try
                    {
                        var mapping = JsonConvert.DeserializeObject<Dictionary<string, List<MediaTypeTransformConfig>>>(settings[Settings.FilenameExtensionMediaTypeMapping]);
                        return mapping;
                    }
                    catch
                    {
                        logger.Log(LogLevel.Warning, $"Could not find deserialize for '{Settings.FilenameExtensionMediaTypeMapping}'");
                        return new Dictionary<string, List<MediaTypeTransformConfig>>();
                    }
                }
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.FilenameExtensionMediaTypeMapping}'");
            return new Dictionary<string, List<MediaTypeTransformConfig>>();
        }

        /// <summary>
        /// Optional setting. Default is an empty list.
        /// </summary>
        /// <returns></returns>
        public static List<ImportCondition> GetImportConditions(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.ImportConditions) && !string.IsNullOrWhiteSpace(settings[Settings.ImportConditions]))
            {
                return JsonConvert.DeserializeObject<List<ImportCondition>>(settings[Settings.ImportConditions]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.ImportConditions}'");
            return new List<ImportCondition>();
        }

        public static int GetInitialAssetLoadLimit(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.InitialAssetLoadLimit) && int.TryParse(settings[Settings.InitialAssetLoadLimit], out int limit))
            {
                return limit;
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.InitialAssetLoadLimit}'. Defaults to '0' (unlimited)");
            return 0;
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
            if (settings.ContainsKey(Settings.LocaleStringLanguagesToSet) && !string.IsNullOrWhiteSpace(settings[Settings.LocaleStringLanguagesToSet]))
            {
                return settings[Settings.LocaleStringLanguagesToSet].ConvertTo<IEnumerable<string>>() ?? new List<string>();
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.LocaleStringLanguagesToSet}'");
            return new List<string>();
        }

        public static Dictionary<string, string> GetLocaleMapping(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.LocaleMappingInriverToBynder) && !string.IsNullOrWhiteSpace(settings[Settings.LocaleMappingInriverToBynder]))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(settings[Settings.LocaleMappingInriverToBynder]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.LocaleMappingInriverToBynder}'");
            return new Dictionary<string, string>();
        }

        public static int GetMaxRetryAttempts(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.MaxRetryAttempts) && int.TryParse(settings[Settings.MaxRetryAttempts], out int maxRetryAttempts))
            {
                return maxRetryAttempts;
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration or parse the value to an number for '{Settings.MaxRetryAttempts}' using default value of {Settings.DefaultMaxRetryAttempts}");

            return Settings.DefaultMaxRetryAttempts;
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

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.MultivalueSeparator}'");
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
            if (settings.ContainsKey(Settings.ResourceSearchType) && settings.TryGetValue(Settings.ResourceSearchType, out string setting))
            {
                return setting.ToEnum<ResourceSearchType>();
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.ResourceSearchType}'. Using default value '{ResourceSearchType.AssetId.ToString()}'");

            // default ResourceSearchType.AssetId for backwards compatiblity
            return ResourceSearchType.AssetId;
        }

        public static bool ShouldAddAssetIdPrefixToFilename(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.AddAssetIdPrefixToFilenameOfNewResource) && settings.TryGetValue(Settings.AddAssetIdPrefixToFilenameOfNewResource, out string setting))
            {
                return string.Equals(setting, true.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.AddAssetIdPrefixToFilenameOfNewResource}'. Using default value '{true}'");

            // default true for backwards compatiblity
            return true;
        }

        public static List<FieldTypeThumbnailMapping> GetFieldTypeThumbnailMappings(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.FieldTypeThumbnailMapping) && !string.IsNullOrWhiteSpace(settings[Settings.FieldTypeThumbnailMapping]))
            {
                return JsonConvert.DeserializeObject<List<FieldTypeThumbnailMapping>>(settings[Settings.FieldTypeThumbnailMapping]);
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration for '{Settings.FieldTypeThumbnailMapping}'");
            return new List<FieldTypeThumbnailMapping>();
        }

        public static int GetMaxUpdatedWorkerCalledCount(Dictionary<string, string> settings, IExtensionLog logger)
        {
            if (settings.ContainsKey(Settings.MaxUpdatesToHandle) && int.TryParse(settings[Settings.MaxUpdatesToHandle], out int maxUpdatesToHandle))
            {
                return maxUpdatesToHandle;
            }

            logger.Log(LogLevel.Verbose, $"Could not find configuration or parse the value to an number for '{Settings.MaxUpdatesToHandle}' using default value of {Settings.DefaultMaxUpdatesToHandle}");

            return Settings.DefaultMaxUpdatesToHandle;
        }

        #endregion Methods
    }
}