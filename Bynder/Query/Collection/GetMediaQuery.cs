// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Bynder.Sdk.Query.Collection
{
    /// <summary>
    /// Query to get media from a collection
    /// </summary>
    public class GetMediaQuery
    {
        #region Properties

        /// <summary>
        /// Id of the collection on which to perform the action
        /// </summary>
        public string CollectionId { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes the class with needed information
        /// </summary>
        /// <param name="collectionId">The id of the collection on which the operation will be performed</param>
        public GetMediaQuery(string collectionId)
        {
            CollectionId = collectionId;
        }

        #endregion Constructors
    }
}