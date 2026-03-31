// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model to represent the thumbnails of a media asset
    /// </summary>
    public class Thumbnails
    {
        #region Properties

        /// <summary>
        /// A dictionary representation of all other derivatives
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JToken> All { get; set; }

        // Onderstaande niet als properties gebruiken want dan komen ze niet in All terecht
        // Dit komen we tegen in de MediaHelper

/*        /// <summary>
        /// Mini thumbnail Url
        /// </summary>
        [JsonProperty("mini")]
        public string Mini { get; set; }

        /// <summary>
        /// Thul thumbnail Url
        /// </summary>
        [JsonProperty("thul")]
        public string Thul { get; set; }

        /// <summary>
        /// Web-image Url
        /// </summary>
        [JsonProperty("webimage")]
        public string WebImage { get; set; }*/

        #endregion Properties
    }
}