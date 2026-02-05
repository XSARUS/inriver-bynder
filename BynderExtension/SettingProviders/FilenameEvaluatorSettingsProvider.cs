using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.SettingProviders
{
    using Config;

    public static class FilenameEvaluatorSettingsProvider
    {
        public static Dictionary<string, string> Create()
        {
            return new Dictionary<string, string>()
            {
                { Settings.RegularExpressionForFileName, string.Empty },
            };
        }
    }
}
