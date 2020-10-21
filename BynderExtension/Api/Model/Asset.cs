using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    using Converters;

    [JsonConverter(typeof(AssetJsonConverter))]
    public class Asset
    {
        #region Fields

        private const string Original = "original";

        #endregion Fields

        #region Properties

        public string Id { get; set; }
        public string IdHash { get; set; }

        public List<MediaItem> MediaItems { get; set; }

        public MetapropertyList MetaProperties { get; set; }

        #endregion Properties

        #region Methods

        public string GetOriginalFileName()
        {
            return MediaItems.FirstOrDefault(x => x.Type.Equals(Original))?.FileName;
        }

        public string GetOriginalMimeType()
        {
            string fileName = MediaItems.FirstOrDefault(x => x.Type.Equals(Original))?.FileName;
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey?.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }

            return mimeType;
        }

        #endregion Methods
    }
}