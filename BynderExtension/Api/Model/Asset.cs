using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    public class Asset
    {
        private const string Original = "original";
        public string Id;

        public List<MediaItem> MediaItems;

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
    }
}