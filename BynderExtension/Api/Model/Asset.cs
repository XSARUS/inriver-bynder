using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Bynder.Api.Model
{
    using Converters;
    using System;

    /// <summary>
    /// You can post changes of the Asset for these fields:
    /// * name
    /// * description
    /// * copyright
    /// * brandId
    /// * tags
    /// * datePublished
    /// * archive
    /// * archiveDate
    /// * limited
    /// * limitedDate
    /// * watermarkDate
    /// * isPublic
    /// </summary>
    [JsonConverter(typeof(AssetJsonConverter))]
    public class Asset
    {

        #region Fields

        private const string Original = "original";

        #endregion Fields

        #region Properties

        /// <summary>
        /// Indicating the archived state of the asset.
        /// </summary>
        public bool Archive { get; set; }

        /// <summary>
        /// Archive datetime. ISO8601 format: yyyy-mm-ddThh:mm:ssZ.
        /// Not received in get, only used in POST
        /// </summary>
        public DateTime? ArchiveDate { get; set; }

        /// <summary>
        /// Id of the brand to save the asset to, can be retrieved using the Retrieve brands and subbrands call.
        /// </summary>
        public string BrandId { get; set; }

        /// <summary>
        /// Copyright information of the asset.
        /// </summary>
        public string Copyright { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        /// <summary>
        /// Publication datetime. ISO8601 format: yyyy-mm-ddThh:mm:ssZ.
        /// </summary>
        public DateTime? DatePublished { get; set; }

        /// <summary>
        /// Asset description.
        /// </summary>
        public string Description { get; set; }

        public List<string> Extension { get; set; }
        public long FileSize { get; set; }
        public int Height { get; set; }
        /// <summary>
        /// Asset id, will return the asset for that id.
        /// </summary>
        public string Id { get; set; }

        public string IdHash { get; set; }
        /// <summary>
        /// Indicating the public state of the asset. Warning irreversible, once changed to true it cannot be changed back.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Indicating the limit state of the asset.
        /// </summary>
        public bool Limited { get; set; }

        /// <summary>
        /// Limit datetime. ISO8601 format: yyyy-mm-ddThh:mm:ssZ.
        /// Not received in get, only used in POST
        /// </summary>
        public DateTime? LimitedDate { get; set; }

        /// <summary>
        /// media items (derivatives)
        /// </summary>
        public List<MediaItem> MediaItems { get; set; }

        /// <summary>
        /// Dictionary with (metaproperty) options to set on the asset. 
        /// Send fields as metaproperty.METAPROPERY_ID with a string of all its (metaproperty) options comma-separated. 
        /// Note that the list of (metaproperty) options should include all the (metaproperty) options available in the lower hierarchy;
        /// meaning it should include the (metaproperty) options of the (metaproperty) options etc.
        /// </summary>
        public MetapropertyList MetaProperties { get; set; }

        /// <summary>
        /// Name of the asset, beware the asset will have no name when this is empty.
        /// </summary>
        public string Name { get; set; }

        public string Orientation { get; set; }
        /// <summary>
        /// Comma-separated tags. Tags will be appended to current tags list. If the tag doesn't exist it will be created.
        /// </summary>
        public List<string> Tags { get; set; }

        public string Type { get; set; }
        public string UserCreated { get; set; }
        /// <summary>
        /// Watermark datetime. ISO8601 format: yyyy-mm-ddThh:mm:ssZ.
        /// Not received in get, only used in POST
        /// </summary>
        public DateTime? WatermarkDate { get; set; }

        /// <summary>
        /// Indicating the watermarked state of the asset.
        /// </summary>
        public bool Watermarked { get; set; }

        public int Width { get; set; }

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