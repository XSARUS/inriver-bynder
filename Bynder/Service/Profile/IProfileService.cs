using Bynder.Query.Profile;
using Bynder.Sdk.Api.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Sdk.Service.Profile
{
    public interface IProfileService
    {
        Task<IList<Model.Profile>> GetProfilesAsync();

        Task<Model.Profile> GetProfileAsync(ProfileQuery query);
    }
}
