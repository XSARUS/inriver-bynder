// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;

namespace Bynder.Sdk.Model
{
    /// <summary>
    /// Token Model returned when OAuth2 flow finishes or when
    /// the access token is refreshed
    /// </summary>
    public class Token
    {
        #region Fields

        private DateTimeOffset _accessTokenExpiration;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the access token. Used to do authenticated requests to the API.
        /// </summary>
        /// <value>The access token.</value>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// The access token expiration.
        /// </summary>
        public DateTimeOffset AccessTokenExpiration
        {
            get { return _accessTokenExpiration; }
        }

        /// <summary>
        /// Gets the number of milliseconds it will take for the access token to expire.
        /// </summary>
        /// <value>Number of milliseconds</value>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets the refresh token. Used to get new access tokens
        /// </summary>
        /// <value>The refresh token.</value>
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        /// <value>The scope.</value>
        [JsonProperty("scope")]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the type of the token.
        /// </summary>
        /// <value>The type of the token.</value>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        #endregion Properties

        #region Methods

        public void SetAccessTokenExpiration()
        {
            _accessTokenExpiration = DateTimeOffset.Now.AddSeconds(ExpiresIn);
        }

        #endregion Methods
    }
}