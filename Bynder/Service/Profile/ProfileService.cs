using Bynder.Query.Profile;
using Bynder.Sdk.Api.Requests;
using Bynder.Sdk.Api.RequestSender;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.Profile
{
    internal class ProfileService : IProfileService
    {
        #region Fields

        /// <summary>
        /// Request sender to communicate with the Bynder API
        /// </summary>
        private readonly IApiRequestSender _requestSender;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the class
        /// </summary>
        /// <param name="requestSender">instance to communicate with the Bynder API</param>
        public ProfileService(IApiRequestSender requestSender)
        {
            _requestSender = requestSender;
        }

        #endregion Constructors

        #region Methods

        public async Task<Model.Profile> GetProfileAsync(ProfileQuery query)
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<Model.Profile>
            {
                Path = $"/api/v4/profiles/{query.Id}",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }

        public async Task<IList<Model.Profile>> GetProfilesAsync()
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<IList<Model.Profile>>
            {
                Path = "/api/v4/profiles/",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }

        #endregion Methods
    }
}