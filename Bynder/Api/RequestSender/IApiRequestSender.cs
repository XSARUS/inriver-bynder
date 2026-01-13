// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.


using Bynder.Sdk.Api.Requests;
using Bynder.Sdk.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bynder.Sdk.Api.RequestSender
{
    /// <summary>
    /// API request sender interface. All requests to Bynder are done using
    /// this request sender.
    /// </summary>
    internal interface IApiRequestSender : IDisposable
    {
        /// <summary>
        /// Sends the request async.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <typeparam name="T">Type we want to deserialize response to.</typeparam>
        /// <returns>The deserialized response.</returns>
        /// <exception cref="T:System.Net.Http.HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.</exception>
        Task<T> SendRequestAsync<T>(Request<T> request);

        Task<IReadOnlyList<Media>> SendCursorRequestAsync(Request<List<Media>> request);
        Task<IReadOnlyList<TItem>> SendPagedRequestAsync<TItem>(
            Request<List<TItem>> request,
            int pageSize = 50,
            int maxPages = int.MaxValue);
    }
}
