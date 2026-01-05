using Bynder.Sdk.Model;
using Bynder.Sdk.Query.Decoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Query.Profile
{

    public class ProfileQuery
    {
        [ApiField("id")]
        public string Id { get; set; }
    }
}
