using System.Collections.Generic;

namespace Bynder.SettingProviders
{
    using Config;

    public static class AssetDownloadWorkerSettingsProvider
    {
        #region Methods

        public static Dictionary<string, string> Create()
        {
            var settings = new Dictionary<string, string>()
            {
                { Settings.FilenameExtensionMediaTypeMapping, string.Empty },
                { Settings.AddAssetIdPrefixToFilenameOfNewResource, true.ToString() },
                { Settings.DownloadMediaType, "original" },
                { Settings.FieldTypeThumbnailMapping, string.Empty  }
            };

            return settings;
        }

        #endregion Methods
    }
}