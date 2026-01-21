// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Bynder.Sdk.Api.Converters;
using System;
using System.Linq;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Media model returned by API /media
    /// </summary>
    public class Media
    {
        /// <summary>
        /// Property options assigned to media
        /// </summary>
        [JsonProperty("propertyOptions")]
        public IList<string> PropertyOptions { get; set; }

        /// <summary>
        /// Property asset types assigned to media
        /// </summary>
        [JsonProperty("property_assettype")]
        [JsonIgnore]
        [Obsolete("Use PropertyOptionsDictionary?[\"property_assettype\"] instead")]
        public IList<string> PropertyAssetType
        {
            get
            {
                if (PropertyOptionsDictionary == null || !PropertyOptionsDictionary.ContainsKey("property_assettype")) { return null; }

                return PropertyOptionsDictionary["property_assettype"].Values().Select(v => v.ToString()).ToList();
            }
        }

        /// <summary>
        /// Active focus point in the original media item, defined
        /// by an x,y coordinate.
        /// </summary>
        [JsonProperty("activeOriginalFocusPoint")]
        public IDictionary<string, int> ActiveOriginalFocusPoint { get; set; }

        /// <summary>
        /// Number of times the media has been downloaded
        /// </summary>
        [JsonProperty("downloads")]
        public int NumberOfDownloads { get; set; }

        /// <summary>
        /// Number of times the media has been viewed
        /// </summary>
        [JsonProperty("views")]
        public int NumberOfViews { get; set; }

        /// <summary>
        /// Id of the brand the media belongs to
        /// </summary>
        [JsonProperty("brandId")]
        public string BrandId { get; set; }

        /// <summary>
        /// Id of the Subbrand the media belongs to
        /// </summary>
        [JsonProperty("subBrandId")]
        public string SubBrandId { get; set; }

        /// <summary>
        /// Full name of the user who created the media
        /// </summary>
        [JsonProperty("userCreated")]
        public string Creator { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        [JsonProperty("dateCreated")]
        public string DateCreated { get; set; }

        /// <summary>
        /// Date modified
        /// </summary>
        [JsonProperty("dateModified")]
        public string DateModified { get; set; }

        /// <summary>
        /// Date modified
        /// </summary>
        [JsonProperty("datePublished")]
        public string DatePublished { get; set; }

        /// <summary>
        /// Extension of the file
        /// </summary>
        [JsonProperty("extension")]
        public IList<string> Extension { get; set; }

        /// <summary>
        /// Height
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// Width
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>
        /// Media id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Media id hash
        /// </summary>
        [JsonProperty("idHash")]
        public string IdHash { get; set; }

        /// <summary>
        /// Media name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Orientation
        /// </summary>
        [JsonProperty("orientation")]
        public string Orientation { get; set; }

        /// <summary>
        /// Media type. Possible values are image, document, audio, video
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Generated thumbnails for the media
        /// </summary>
        [JsonProperty("thumbnails")]
        public Thumbnails Thumbnails { get; set; }

        /// <summary>
        /// Video preview Urls
        /// </summary>
        [JsonProperty("videoPreviewURLs")]
        public IList<string> VideoPreviewURLs { get; set; }

        /// <summary>
        /// Multiple media items for a media. Including derivatives, additional and original.
        /// To get this information we have to call <see cref="RequestMediaInfoAsync"/> with the media id.
        /// </summary>
        [JsonProperty("mediaItems")]
        public IList<MediaItem> MediaItems { get; set; }

        /// <summary>
        /// Current active version
        /// </summary>
        [JsonProperty("activeVersion")]
        public int ActiveVersion { get; set; }

        /// <summary>
        /// Copyright of the media
        /// </summary>
        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// File size of the original in bytes
        /// </summary>
        [JsonProperty("fileSize")]
        public long FileSize { get; set; }

        /// <summary>
        /// Tags of the media item
        /// </summary>
        [JsonProperty("tags")]
        public IList<string> Tags { get; set; }

        /// <summary>
        /// Indicates if the media item is archived
        /// </summary>
        [JsonProperty("archive", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsArchived { get; set; }

        /// <summary>
        /// Indicates if the media item is marked as limited usage
        /// </summary>
        [JsonProperty("limited", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsLimited { get; set; }

        /// <summary>
        /// Indicates if the media item is public
        /// </summary>
        [JsonProperty("isPublic", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Indicates if the media item is watermarked
        /// </summary>
        [JsonProperty("watermarked", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsWatermarked { get; set; }

        /// <summary>
        /// URL to Bynder CDN for the original
        /// </summary>
        [JsonProperty("original")]
        public string Original { get; set; }

        /// <summary>
        /// Dynamic Asset Transformation base URL.
        /// </summary>
        [JsonProperty("transformBaseUrl")]
        public string TransformBaseUrl { get; set; }

        /// <summary>
        /// A dictionary representation of properties
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JToken> PropertyOptionsDictionary { get; set; }

        /////////////////////////////////////////////////
        /// EXTRA XSARUS PROPERTIES, FIELDS & METHODS ///
        /////////////////////////////////////////////////
        private const string _propertyPrefix = "property_";
        private AssetMetapropertyList _metaProperties;

        /// <summary>
        /// Additional Dictionary with (metaproperty) options to set on the asset.
        /// Send fields as metaproperty.METAPROPERY_ID with a string of all its (metaproperty) options comma-separated.
        /// Note that the list of (metaproperty) options should include all the (metaproperty) options available in the lower hierarchy;
        /// meaning it should include the (metaproperty) options of the (metaproperty) options etc.
        /// </summary>
        public AssetMetapropertyList MetaProperties { 
            get {
                if (_metaProperties == null)
                {
                    _metaProperties = SetMetapropertyList(PropertyOptionsDictionary);
                }

                return _metaProperties;
            }
        }

        public static AssetMetapropertyList SetMetapropertyList(Dictionary<string, JToken> properties)
        {
            var propertyTokens = properties.Where(x => x.Key.StartsWith(_propertyPrefix));
            var metaProperties = propertyTokens.Select(jProperty =>
                 // property values are always send as array
                 new AssetMetaproperty
                 {
                     Name = jProperty.Key.Substring(jProperty.Key.IndexOf(_propertyPrefix) + _propertyPrefix.Length),
                     Values = GetValueAsStringList(jProperty.Value)
                 });
            return new AssetMetapropertyList(metaProperties);
        }

        public static List<string> GetValueAsStringList(JToken token)
        {
            if (token == null) return new List<string>();

            switch (token.Type)
            {
                case JTokenType.Null:
                    return new List<string>();

                case JTokenType.Array:
                    var arr = (JArray)token;
                    return arr.ToObject<List<string>>();

                // We do not need to implement this for now.
                // It depends on what will be send as value for the metaproperty types in bynder. We have only seen strings and string arrays.
                case JTokenType.Object:
                    throw new NotImplementedException("No implementation to process JToken Object yet");

                default:
                    return new List<string> { token.Value<string>() };
            }
        }

        /// <summary>
        /// Additional method to get the original mimetype
        /// </summary>
        /// <returns></returns>
        public string GetOriginalMimeType()
        {
            string fileName = GetOriginalFileName();
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

        /// <summary>
        /// Additional method to get the original filename
        /// </summary>
        /// <returns></returns>
        public string GetOriginalFileName()
        {
            return MediaItems.FirstOrDefault(x => x.Type.Equals("original"))?.Name;
        }
    }
}
