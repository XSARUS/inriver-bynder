using Bynder.Query.User;
using Bynder.Sdk.Api.Requests;
using Bynder.Sdk.Api.RequestSender;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.User
{
    internal class UserService : IUserService
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
        public UserService(IApiRequestSender requestSender)
        {
            _requestSender = requestSender;
        }

        #endregion Constructors

        #region Methods

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

        public async Task<IList<Model.User>> GetUsersAsync(UsersQuery query)
        {
            return await _requestSender.SendRequestAsync(new ApiRequest<IList<Model.User>>
            {
                Path = "/api/v4/users",
                HTTPMethod = HttpMethod.Get,
                Query = query,
            }).ConfigureAwait(false);
        }

        #endregion Methods
    }
}