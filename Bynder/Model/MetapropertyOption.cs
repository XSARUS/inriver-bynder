// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Sdk.Api.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model to represent metaproperty options
    /// </summary>
    public class MetapropertyOption
    {
        #region Properties

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("displayLabel")]
        public string DisplayLabel { get; set; }

        /// <summary>
        /// Id of the option
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// True if metaproperty option has selectable turned on
        /// </summary>
        [JsonProperty("isSelectable", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsSelectable { get; set; }

        /// <summary>
        /// Label of the option
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        // Added to the SDK variant of this Model
        [JsonProperty("labels")]
        public Dictionary<string, string> Labels { get; set; }

        /// <summary>
        /// Id's of the linked metaproperty options
        /// </summary>
        [JsonProperty("linkedOptionIds")]
        public List<string> LinkedOptionIds { get; set; }

        /// <summary>
        /// Name of the option
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Child metaproperty options
        /// </summary>
        [JsonProperty("options")]
        public List<MetapropertyOption> Options { get; set; }

        /// <summary>
        /// Order in which option should appear
        /// </summary>
        [JsonProperty("zindex")]
        public string ZIndex { get; set; }

        #endregion Properties
    }
}