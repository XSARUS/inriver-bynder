// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Query.Asset;
using Bynder.Sdk.Query.Decoder;

namespace Bynder.Sdk.Query.Asset
{
    /// <summary>
    /// Query to filter media results, including the option to see the total number of results
    /// </summary>
    public class MediaQuerySearch : MediaQueryFull, ICursorPaginatedRequest
    {
        #region Properties

        [ApiField("count")]
        public bool Count { get; set; }

        [ApiField("cursor")]
        public string Cursor { get; private set; }

        #endregion Properties

        #region Methods

        public void SetCursor(string cursor)
        {
            Cursor = cursor;
        }

        #endregion Methods
    }
}