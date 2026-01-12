using inRiver.Remoting.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Utils.Helpers
{
    using Bynder.Api.Model;
    using Bynder.Sdk.Model;
    using Bynder.Sdk.Query.Asset;
    using SdkIBynderClient = Bynder.Sdk.Service.IBynderClient;

    public class BynderHelper
    {
        private SdkIBynderClient _bynderClient { get; set; }

        public BynderHelper(SdkIBynderClient bynderClient = null)
        {
            _bynderClient = bynderClient;
        }

        /*public async Task<Media> GetAssetByMediaQuery(string mediaId)
        {
            return await _bynderClient
                .GetAssetService()
                .GetMediaInfoAsync(new MediaInformationQuery() { 
                    MediaId = mediaId,
                    Versions = 1
                })
                .ConfigureAwait(false);
        }*/
    }
}
