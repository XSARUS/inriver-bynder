using Bynder.Query.Profile;
using Bynder.Sdk.Api.Requests;
using Bynder.Sdk.Api.RequestSender;
using Bynder.Sdk.Model;
using Bynder.Sdk.Service.Asset;
using Bynder.Sdk.Service.Upload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.Profile
{
    internal class ProfileService : IProfileService
    {
        /// <summary>
        /// Request sender to communicate with the Bynder API
        /// </summary>
        private readonly IApiRequestSender _requestSender;

        /// <summary>
        /// Initializes a new instance of the class
        /// </summary>
        /// <param name="requestSender">instance to communicate with the Bynder API</param>
        public ProfileService(IApiRequestSender requestSender)
        {
            _requestSender = requestSender;
        }

        public async Task<IList<Model.Profile>> GetProfilesAsync()
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<IList<Model.Profile>>
            {
                Path = "/api/v4/profiles/",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }

        public async Task<Model.Profile> GetProfileAsync(ProfileQuery query)
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<Model.Profile>
            {
                Path = $"/api/v4/profiles/{query.Id}",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }
    }
}
