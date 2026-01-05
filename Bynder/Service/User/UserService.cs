using Bynder.Query.Profile;
using Bynder.Query.User;
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

namespace Bynder.Sdk.Service.User
{
    internal class UserService : IUserService
    {
        /// <summary>
        /// Request sender to communicate with the Bynder API
        /// </summary>
        private readonly IApiRequestSender _requestSender;

        /// <summary>
        /// Initializes a new instance of the class
        /// </summary>
        /// <param name="requestSender">instance to communicate with the Bynder API</param>
        public UserService(IApiRequestSender requestSender)
        {
            _requestSender = requestSender;
        }

        public async Task<IList<Model.User>> GetUsersAsync(UsersQuery query)
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<IList<Model.User>>
            {
                Path = "/api/v4/users",
                HTTPMethod = HttpMethod.Get,
                Query = query,
            }).ConfigureAwait(false);
        }

        public async Task<Model.User> GetCurrentUserAsync()
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<Model.User>
            {
                Path = "/api/v4/currentUser",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }

        public async Task<Model.User> GetUserAsync(UserQuery query)
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<Model.User>
            {
                Path = $"/api/v4/users/{query.Id}",
                HTTPMethod = HttpMethod.Get,
            }).ConfigureAwait(false);
        }
    }
}
