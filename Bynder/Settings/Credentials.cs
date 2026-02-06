// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Sdk.Model;
using System;

namespace Bynder.Sdk.Settings
{
    /// <summary>
    /// Credentials implementation.
    /// </summary>
    internal class Credentials : ICredentials
    {
        #region Fields

        private Token _token;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        /// <value>Check <see cref="t:ICredentials"/>.</value>
        public string AccessToken => _token?.AccessToken;

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        /// <value>Check <see cref="t:ICredentials"/>.</value>
        public string RefreshToken => _token?.RefreshToken;

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        /// <value>Check <see cref="t:ICredentials"/>.</value>
        public string TokenType => _token?.TokenType ?? "Bearer";

        /// <summary>
        /// Gets or sets the token that will be used to authenticate API calls.
        /// </summary>
        /// <value>The token.</value>
        private Token Token
        {
            get { return _token; }

            set
            {
                if (value != _token)
                {
                    _token = value;
                    OnCredentialsChanged?.Invoke(this, _token);
                }
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        /// <returns>Check <see cref="t:ICredentials"/>.</returns>
        public bool AreValid()
        {
            if (_token != null
                && _token.AccessToken != null)
            {
                var limitExpiration = DateTimeOffset.UtcNow.AddSeconds(15);
                return _token.AccessTokenExpiration > limitExpiration;
            }

            return false;
        }

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        /// <param name="token">Check <see cref="t:ICredentials"/>.</param>
        public void Update(Token token)
        {
            if (Token != null)
            {
                token.RefreshToken = Token.RefreshToken;
            }
            token.SetAccessTokenExpiration();
            Token = token;
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// Check <see cref="t:ICredentials"/>.
        /// </summary>
        public event EventHandler<Token> OnCredentialsChanged;

        #endregion Events
    }
}