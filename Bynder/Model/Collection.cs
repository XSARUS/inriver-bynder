// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model describing a collection
    /// </summary>
    public class Collection
    {
        #region Properties

        /// <summary>
        /// Cover of collection
        /// </summary>
        [JsonProperty("cover")]
        public CollectionCover Cover { get; set; }

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
        /// Description of collection
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Id of collection
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Flags if collection is public
        /// </summary>
        [JsonProperty("IsPublic")]
        public bool IsPublic { get; set; }

        /// <summary>
        /// Link to collection
        /// </summary>
        [JsonProperty("link")]
        public Uri Link { get; set; }

        /// <summary>
        /// Number of media in collection
        /// </summary>
        [JsonProperty("collectionCount")]
        public int MediaCount { get; set; }

        /// <summary>
        /// Name of collection
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// User id logged in
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Thumbnail url of the media collection
        /// Exists when retrieving a specific collection. For consistency, the Thumbnail of the <see cref="Cover"/> is populated with this value.
        /// </summary>
        [JsonProperty("thumbnail")]
        private Uri Thumbnail
        {
            set
            {
                if (Cover == null)
                {
                    Cover = new CollectionCover();
                }

                Cover.Thumbnail = value;
            }
        }

        #endregion Properties
    }
}