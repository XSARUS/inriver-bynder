using Bynder.Query.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.User
{
    public interface IUserService
    {
        #region Methods

        Task<Model.User> GetCurrentUserAsync();

        Task<Model.User> GetUserAsync(UserQuery query);

        Task<IList<Model.User>> GetUsersAsync(UsersQuery query);

        #endregion Methods
    }
}