// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Sdk.Api.Converters;
using Bynder.Sdk.Query.Decoder;
using System.Collections.Generic;

namespace Bynder.Sdk.Query.Collection
{
    /// <summary>
    /// Query used to add media to a collection
    /// </summary>
    public class AddMediaQuery
    {
        #region Properties

        /// <summary>
        /// Id of the collection on which to perform the action
        /// </summary>
        public string CollectionId { get; private set; }

        /// <summary>
        /// List with the Ids of the media
        /// </summary>
        [ApiField("data", Converter = typeof(JsonConverter))]
        public IList<string> MediaIds { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes the class with needed information
        /// </summary>
        /// <param name="collectionId">The id of the collection on which the operation will be performed</param>
        /// <param name="mediaIds">List with the Ids of the media</param>
        public AddMediaQuery(string collectionId, IList<string> mediaIds)
        {
            CollectionId = collectionId;
            MediaIds = mediaIds;
        }

        #endregion Constructors
    }
}