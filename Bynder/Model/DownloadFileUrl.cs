// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Model returned by media_id/download/item_id
    /// </summary>
    public class DownloadFileUrl
    {
        #region Properties

        /// <summary>
        /// Url of the asset
        /// </summary>
        [JsonProperty("s3_file")]
        public Uri S3File { get; set; }

        #endregion Properties
    }
}