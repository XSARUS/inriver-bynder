// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Query.Asset;
using Bynder.Sdk.Api.Requests;
using Bynder.Sdk.Extensions;
using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Query.Decoder;
using Bynder.Sdk.Service;
using Bynder.Sdk.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Bynder.Sdk.Api.RequestSender
{
    /// <summary>
    /// Implementation of <see cref="IApiRequestSender"/> interface.
    /// </summary>
    internal class ApiRequestSender : IApiRequestSender
    {
        #region Fields

        private readonly IBynderClient _bynderClient;
        private readonly Configuration _configuration;
        private readonly ICredentials _credentials;
        private readonly QueryDecoder _queryDecoder = new QueryDecoder();
        private IHttpRequestSender _httpSender;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sdk.Api.ApiRequestSender"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="credentials">Credentials to use in authorized requests and to refresh tokens</param>
        /// <param name="oauthService">OAuthService.</param>
        /// <param name="httpSender">HTTP instance to send API requests</param>
        internal ApiRequestSender(Configuration configuration, ICredentials credentials, IBynderClient bynderClient, IHttpRequestSender httpSender)
        {
            _configuration = configuration;
            _credentials = credentials;
            _bynderClient = bynderClient;
            _httpSender = httpSender;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Create an instance of <see cref="IApiRequestSender"/> given the specified configuration and credentials.
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="configuration">Configuration.</param>
        /// <param name="credentials">Credentials.</param>
        /// <param name="oauthService">OAuthService.</param>
        public static IApiRequestSender Create(Configuration configuration, ICredentials credentials, IBynderClient bynderClient)
        {
            return new ApiRequestSender(configuration, credentials, bynderClient, new HttpRequestSender());
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Sdk.Api.ApiRequestSender"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:Sdk.Api.ApiRequestSender"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:Sdk.Api.ApiRequestSender"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:Sdk.Api.ApiRequestSender"/> so the garbage collector can reclaim the memory that the
        /// <see cref="T:Sdk.Api.ApiRequestSender"/> was occupying.</remarks>
        public void Dispose()
        {
            _httpSender.Dispose();
        }

        public async Task<IReadOnlyList<Media>> SendCursorRequestAsync(
                    Request<List<Media>> request)
        {
            throw new NotImplementedException("This needs solr-cursor activated!");
            var results = new List<Media>();

            // Cursor moet expliciet leeg starten
            if (request.Query is ICursorPaginatedRequest cprInit)
            {
                cprInit.SetCursor(null);
            }

            // Cursor-queries mogen NOOIT page / total hebben
            if (request.Query is MediaQuerySearch mq)
            {
                mq.Page = null;
                mq.Total = false;
                if (mq.Limit == null)
                {
                    mq.Limit = 50;
                }
            }

            string cursor;

            do
            {
                var response = await CreateHttpRequestAsync(request)
                    .ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                var wrapper = JsonConvert.DeserializeObject<MediaResponse>(json);

                if (wrapper?.Media != null && wrapper.Media.Count > 0)
                {
                    results.AddRange(wrapper.Media);
                }

                cursor = GetNextCursor(response);

                if (!string.IsNullOrEmpty(cursor) &&
                    request.Query is ICursorPaginatedRequest cpr)
                {
                    cpr.SetCursor(cursor);
                }
            } while (!string.IsNullOrEmpty(cursor));

            return results;
        }

        public async Task<IReadOnlyList<TItem>> SendPagedRequestAsync<TItem>(
                    Request<List<TItem>> request,
                    int pageSize = 50,
                    int maxPages = int.MaxValue)
        {
            if (request.HTTPMethod != HttpMethod.Get)
                throw new InvalidOperationException("Paged requests only support GET.");

            var results = new List<TItem>();
            int page = 1;

            // Zorg dat we met een schone query starten
            if (request.Query is MediaQuery mq)
            {
                mq.Page = null;
                mq.Limit = pageSize;
            }

            while (page <= maxPages)
            {
                if (request.Query is MediaQuery q)
                {
                    q.Page = page;
                    q.Limit = pageSize;
                }

                var response = await CreateHttpRequestAsync(request)
                    .ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                var batch = JsonConvert.DeserializeObject<List<TItem>>(json);

                if (batch == null || batch.Count == 0)
                    break;

                results.AddRange(batch);

                // Stopconditie: laatste pagina
                if (batch.Count < pageSize)
                    break;

                page++;
            }

            return results;
        }

        /// <summary>
        /// Check <see cref="t:Sdk.Api.IApiRequestSender"/>.
        /// </summary>
        /// <param name="request">Check <see cref="t:Sdk.Api.IApiRequestSender"/>.</param>
        /// <typeparam name="T">Check <see cref="t:Sdk.Api.IApiRequestSender"/>.</typeparam>
        /// <returns>Check <see cref="t:Sdk.Api.IApiRequestSender"/>.</returns>
        /// <exception cref="T:System.Net.Http.HttpRequestException">Check <see cref="t:Sdk.Api.IApiRequestSender"/>.</exception>
        public async Task<T> SendRequestAsync<T>(Request<T> request)
        {
            var response = await CreateHttpRequestAsync(request).ConfigureAwait(false);
            var responseContent = response.Content;
            if (response.Content == null)
            {
                return default;
            }

            var responseString = await responseContent.ReadAsStringAsync().ConfigureAwait(false);
            if (responseString == null)
            {
                return default;
            }
            // Note: for powerpoints, a pdf is automatically generated by Bynder and stored as a second MediaItem.
            // However, this MediaItem has a Height and Width of null, which cannot be converted to an int so it breaks.
            // The below setting fixes this
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(responseString, settings);
        }

        private static string GetNextCursor(HttpResponseMessage response)
        {
            return response.Headers.TryGetValues("X-Bynder-NextCursor", out var values)
                ? values.FirstOrDefault()
                : null;
        }

        private async Task<HttpResponseMessage> CreateHttpRequestAsync<T>(Request<T> request)
        {
            var parameters = _queryDecoder.GetParameters(request.Query);

            var httpRequestMessage = HttpRequestMessageFactory.Create(
                _configuration.BaseUrl.ToString(),
                request.HTTPMethod,
                parameters,
                request.Path
            );

            if (request.Authenticated)
            {
                if (!_credentials.AreValid())
                {
                    // Get a refesh token when the credentials are no longer valid
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await _bynderClient.GetOAuthService().GetRefreshTokenAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                    _credentials.TokenType,
                    _credentials.AccessToken
                );
            }

            return await _httpSender.SendHttpRequest(httpRequestMessage).ConfigureAwait(false);
        }

        #endregion Methods

        /**
         * Not implemented since this only can work if solr-cursor is activated on your account
         */

        #region Classes

        public class MediaResponse
        {
            #region Properties

            [JsonProperty("count")]
            public int Count { get; set; }

            [JsonProperty("limited")]
            public bool Limited { get; set; }

            [JsonProperty("media")]
            public List<Media> Media { get; set; }

            #endregion Properties
        }

        private static class HttpRequestMessageFactory
        {
            #region Methods

            internal static HttpRequestMessage Create(
                string baseUrl,
                HttpMethod method,
                IDictionary<string, string> requestParams,
                string urlPath)
            {
                var builder = new UriBuilder(baseUrl).AppendPath(urlPath);

                if (HttpMethod.Get == method || HttpMethod.Delete == method)
                {
                    builder.Query = Utils.Url.ConvertToQuery(requestParams);
                }

                HttpRequestMessage requestMessage = new HttpRequestMessage(method, builder.ToString());

                if (HttpMethod.Post == method)
                {
                    requestMessage.Content = new FormUrlEncodedContent(requestParams);
                }

                return requestMessage;
            }

            #endregion Methods
        }

        #endregion Classes
    }
}