using Bynder.Query.Profile;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.Profile
{
    public interface IProfileService
    {
        #region Methods

        Task<Model.Profile> GetProfileAsync(ProfileQuery query);

        Task<IList<Model.Profile>> GetProfilesAsync();

        #endregion Methods
    }
}