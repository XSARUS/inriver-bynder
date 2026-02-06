// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model describing the cover of a collection
    /// </summary>
    public class CollectionCover
    {
        #region Properties

        /// <summary>
        /// Url to the large version of the cover
        /// </summary>
        [JsonProperty("large")]
        public Uri Large { get; set; }

        /// <summary>
        /// Thumbnail Url
        /// </summary>
        [JsonProperty("thumbnail")]
        public Uri Thumbnail { get; set; }

        /// <summary>
        /// Thumbnail Urls
        /// </summary>
        [JsonProperty("thumbnails")]
        public IEnumerable<Uri> Thumbnails { get; set; }

        #endregion Properties
    }
}