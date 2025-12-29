using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Webdam.Sdk.Service;
using Webdam.Sdk.Settings;

namespace Bynder.Api
{
    public class BynderOauth2Client
    {
        private readonly string _customerBynderUrl;

        public BynderOauth2Client(BynderClientSettings settings)
        {
            _customerBynderUrl = settings.CustomerBynderUrl;

            


            // InitializeManager(settings.ConsumerKey, settings.ConsumerSecret, settings.Token, settings.TokenSecret);
        }
    }
}
