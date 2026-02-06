// Copyright (c) Bynder. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bynder.Sdk.Model;
using Bynder.Sdk.Service.Asset;
using Bynder.Sdk.Service.Collection;
using Bynder.Sdk.Service.OAuth;
using Bynder.Sdk.Service.Profile;
using Bynder.Sdk.Service.User;
using System;

namespace Bynder.Sdk.Service
{
    /// <summary>
    /// Bynder Client interface.
    /// </summary>
    public interface IBynderClient : IDisposable
    {
        #region Methods

        /// <summary>
        /// Gets the asset service to interact with assets in your Bynder portal.
        /// </summary>
        /// <returns>The asset service.</returns>
        IAssetService GetAssetService();

        /// <summary>
        /// Gets the collection service to interact with collections in your Bynder portal.
        /// </summary>
        /// <returns>The collection service.</returns>
        ICollectionService GetCollectionService();

        /// <summary>
        /// Gets the OAuth service.
        /// </summary>
        /// <returns>The OAuth service.</returns>
        IOAuthService GetOAuthService();

        /// <summary>
        /// Gets the Profile service
        /// </summary>
        /// <returns>The Profile service.</returns>
        IProfileService GetProfileService();

        /// <summary>
        /// Gets the User service
        /// </summary>
        /// <returns>The User service</returns>
        IUserService GetUserService();

        #endregion Methods

        #region Events

        /// <summary>
        /// Occurs when credentials changed, and that happens every time
        /// the access token is refreshed.
        /// </summary>
        event EventHandler<Token> OnCredentialsChanged;

        #endregion Events
    }
}