using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;

    public static class CvlSyncWorkerSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            return new Dictionary<string, string>()
            {
                { Settings.CvlMetapropertyMapping, "{\"cvl1\":[\"metapropertyguid1\", \"metapropertyguid2\"], \"cvl2\":[\"metapropertyguid3\", \"metapropertyguid4\"]}" },
                { Settings.LocaleMappingInriverToBynder, string.Empty },
                { Settings.BynderLocaleForMetapropertyOptionLabel, string.Empty },
            };
        }
    }
}
