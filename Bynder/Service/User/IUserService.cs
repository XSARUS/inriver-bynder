using Bynder.Query.User;
using Bynder.Sdk.Api.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.User
{
    public interface IUserService
    {
        Task<Model.User> GetCurrentUserAsync();

        Task<Model.User> GetUserAsync(UserQuery query);

        Task<IList<Model.User>> GetUsersAsync(UsersQuery query);
    }
}
